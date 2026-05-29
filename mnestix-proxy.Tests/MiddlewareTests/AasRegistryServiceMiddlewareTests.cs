using mnestix_proxy.Services.Shared;
using mnestix_proxy.Tests.TestMockService;
using System.Text;

namespace mnestix_proxy.Tests.MiddlewareTests
{
    [TestFixture]
    public class AasRegistryServiceMiddlewareTests : IDisposable
    {
        private DownstreamService _mockDownstream = null!;
        private RegistryMockService _mockRegistry = null!;
        private HttpClient _httpClientEnabled = null!;
        private HttpClient _httpClientDisabled = null!;

        private const string AasId = "urn:example:test-aas";
        private const string AssetId = "urn:example:test-asset";

        private static readonly string ValidAasBody = """
            {
                "modelType": "AssetAdministrationShell",
                "id": "urn:example:test-aas",
                "assetInformation": {
                    "globalAssetId": "urn:example:test-asset",
                    "assetKind": "Instance"
                }
            }
            """;

        [OneTimeSetUp]
        public void Setup()
        {
            _mockDownstream = new DownstreamService();
            _mockRegistry = new RegistryMockService();

            var enabledFactory = new IntegrationTestBase(_mockDownstream.Url!, new Dictionary<string, string>
            {
                { "Features:AasRegistryMiddleware", "true" },
                { "ReverseProxy:Clusters:aasRegistryCluster:Destinations:destination1:Address", _mockRegistry.Url! }
            });
            _httpClientEnabled = enabledFactory.CreateClient();
            _httpClientEnabled.DefaultRequestHeaders.Add("X-API-KEY", "verySecureApiKeyMock");

            var disabledFactory = new IntegrationTestBase(_mockDownstream.Url!, new Dictionary<string, string>
            {
                { "Features:AasRegistryMiddleware", "false" },
                { "ReverseProxy:Clusters:aasRegistryCluster:Destinations:destination1:Address", _mockRegistry.Url! }
            });
            _httpClientDisabled = disabledFactory.CreateClient();
            _httpClientDisabled.DefaultRequestHeaders.Add("X-API-KEY", "verySecureApiKeyMock");
        }

        [SetUp]
        public void ClearReceivedRequests()
        {
            _mockRegistry.Clear();
        }

        [Test]
        public async Task Should_Call_Registry_When_POST_To_Repo_With_AAS_Body()
        {
            var content = new StringContent(ValidAasBody, Encoding.UTF8, "application/json");

            await _httpClientEnabled.PostAsync("/repo/shells", content);

            await WaitForRegistryCallAsync(() => _mockRegistry.ReceivedRequests.Any(r =>
                r.Method == "POST" && r.Path.StartsWith("/shell-descriptors")));

            Assert.That(_mockRegistry.ReceivedRequests, Has.Some.Matches<ReceivedRequest>(r =>
                r.Method == "POST" && r.Path.StartsWith("/shell-descriptors")));
        }

        [Test]
        public async Task Should_Call_Registry_When_PUT_To_Repo_With_AAS_Body()
        {
            var b64AasId = Base64StringDeAndEncoder.EncodeTo64(AasId);
            var content = new StringContent(ValidAasBody, Encoding.UTF8, "application/json");

            await _httpClientEnabled.PutAsync($"/repo/shells/{b64AasId}", content);

            await WaitForRegistryCallAsync(() => _mockRegistry.ReceivedRequests.Any(r =>
                r.Method == "POST" && r.Path.StartsWith("/shell-descriptors")));

            Assert.That(_mockRegistry.ReceivedRequests, Has.Some.Matches<ReceivedRequest>(r =>
                r.Method == "POST" && r.Path.StartsWith("/shell-descriptors")));
        }

        [Test]
        public async Task Should_Call_Registry_When_DELETE_To_Repo()
        {
            var b64AasId = Base64StringDeAndEncoder.EncodeTo64(AasId);

            await _httpClientEnabled.DeleteAsync($"/repo/shells/{b64AasId}");

            await WaitForRegistryCallAsync(() => _mockRegistry.ReceivedRequests.Any(r =>
                r.Method == "DELETE" && r.Path.Contains(b64AasId)));

            Assert.That(_mockRegistry.ReceivedRequests, Has.Some.Matches<ReceivedRequest>(r =>
                r.Method == "DELETE" && r.Path.Contains(b64AasId)));
        }

        [Test]
        public async Task Should_Not_Call_Registry_When_POST_To_Repo_With_Non_AAS_Body()
        {
            const string nonAasBody = """{"modelType": "Submodel", "id": "urn:submodel:1"}""";
            var content = new StringContent(nonAasBody, Encoding.UTF8, "application/json");

            await _httpClientEnabled.PostAsync("/repo/shells", content);

            await Task.Delay(500);

            Assert.That(_mockRegistry.ReceivedRequests, Is.Empty);
        }

        [Test]
        public async Task Should_Not_Call_Registry_When_Feature_Flag_Is_Disabled()
        {
            var content = new StringContent(ValidAasBody, Encoding.UTF8, "application/json");

            await _httpClientDisabled.PostAsync("/repo/shells", content);

            await Task.Delay(500);

            Assert.That(_mockRegistry.ReceivedRequests, Is.Empty);
        }

        [OneTimeTearDown]
        public void Dispose()
        {
            _httpClientEnabled.Dispose();
            _httpClientDisabled.Dispose();
            _mockDownstream.Dispose();
            _mockRegistry.Dispose();
        }

        /// <summary>
        /// Polls until the condition is true or the timeout expires.
        /// Needed because registry calls are fire-and-forget and complete after the HTTP response is sent.
        /// </summary>
        private static async Task WaitForRegistryCallAsync(Func<bool> condition, int timeoutMs = 2000)
        {
            var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
            while (DateTime.UtcNow < deadline)
            {
                if (condition()) return;
                await Task.Delay(100);
            }
        }
    }
}
