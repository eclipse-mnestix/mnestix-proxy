using mnestix_proxy.Services.Shared;
using mnestix_proxy.Tests.TestMockService;
using Newtonsoft.Json.Linq;
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

        private static readonly string FullAasBody = """
            {
                "modelType": "AssetAdministrationShell",
                "id": "urn:example:test-aas",
                "idShort": "TestAas",
                "description": [{"language": "en", "text": "Test AAS description"}],
                "displayName": [{"language": "en", "text": "Test AAS"}],
                "administration": {"version": "1", "revision": "0"},
                "assetInformation": {
                    "globalAssetId": "urn:example:test-asset",
                    "assetKind": "Instance",
                    "assetType": "TestType",
                    "specificAssetIds": [{"name": "serialNumber", "value": "12345"}]
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

        [Test]
        public async Task Should_POST_Full_Descriptor_With_All_Fields_Mapped_Correctly()
        {
            var content = new StringContent(FullAasBody, Encoding.UTF8, "application/json");

            await _httpClientEnabled.PostAsync("/repo/shells", content);

            await WaitForRegistryCallAsync(() => _mockRegistry.ReceivedRequests.Any(r =>
                r.Method == "POST" && r.Path.StartsWith("/shell-descriptors")));

            var request = _mockRegistry.ReceivedRequests.First(r =>
                r.Method == "POST" && r.Path.StartsWith("/shell-descriptors"));
            var descriptor = JObject.Parse(request.Body!);
            var expectedDescription = JArray.Parse("""[{"language":"en","text":"Test AAS description"}]""");
            var expectedDisplayName = JArray.Parse("""[{"language":"en","text":"Test AAS"}]""");
            var expectedSpecificAssetIds = JArray.Parse("""[{"name":"serialNumber","value":"12345"}]""");

            Assert.Multiple(() =>
            {
                Assert.That(request.Path, Is.EqualTo("/shell-descriptors"), "request path");

                // Core identity
                Assert.That(descriptor["id"]?.Value<string>(), Is.EqualTo(AasId), "id");
                Assert.That(descriptor["globalAssetId"]?.Value<string>(), Is.EqualTo(AssetId), "globalAssetId");

                // Pass-through metadata fields
                Assert.That(descriptor["idShort"]?.Value<string>(), Is.EqualTo("TestAas"), "idShort");
                Assert.That(JToken.DeepEquals(descriptor["description"], expectedDescription), Is.True, "description");
                Assert.That(JToken.DeepEquals(descriptor["displayName"], expectedDisplayName), Is.True, "displayName");
                Assert.That(descriptor["administration"]?["version"]?.Value<string>(), Is.EqualTo("1"), "administration.version");
                Assert.That(descriptor["administration"]?["revision"]?.Value<string>(), Is.EqualTo("0"), "administration.revision");

                // Flattened assetInformation fields
                Assert.That(descriptor["assetKind"]?.Value<string>(), Is.EqualTo("Instance"), "assetKind");
                Assert.That(descriptor["assetType"]?.Value<string>(), Is.EqualTo("TestType"), "assetType");
                Assert.That(JToken.DeepEquals(descriptor["specificAssetIds"], expectedSpecificAssetIds), Is.True, "specificAssetIds");
                Assert.That(descriptor["assetInformation"], Is.Null, "assetInformation should be flattened away");

                // modelType must NOT be in the registry descriptor
                Assert.That(descriptor["modelType"], Is.Null, "modelType should not be present in descriptor");

                // Endpoint pointing to the proxy's /repo/shells/{base64Id}
                var endpoints = descriptor["endpoints"] as JArray;
                Assert.That(endpoints, Has.Count.EqualTo(1), "endpoints array");
                var endpoint = endpoints![0];
                Assert.That(endpoint["interface"]?.Value<string>(), Is.EqualTo("AAS-3.0"), "endpoint interface");
                var href = endpoint["protocolInformation"]?["href"]?.Value<string>();
                Assert.That(href, Does.Contain("/repo/shells/"), "endpoint href should contain /repo/shells/");
                Assert.That(href, Does.Contain(Base64StringDeAndEncoder.EncodeTo64(AasId)),
                    "endpoint href should contain the base64-encoded AAS id");
                Assert.That(endpoint["protocolInformation"]?["endpointProtocol"]?.Value<string>(), Is.EqualTo("HTTP"),
                    "endpoint protocol");
                Assert.That(JToken.DeepEquals(endpoint["protocolInformation"]?["endpointProtocolVersion"], new JArray("1.1")),
                    Is.True, "endpoint protocol version");
            });
        }

        [Test]
        public async Task Should_POST_Descriptor_With_Only_Required_Fields_When_AAS_Body_Is_Minimal()
        {
            var content = new StringContent(ValidAasBody, Encoding.UTF8, "application/json");

            await _httpClientEnabled.PostAsync("/repo/shells", content);

            await WaitForRegistryCallAsync(() => _mockRegistry.ReceivedRequests.Any(r =>
                r.Method == "POST" && r.Path.StartsWith("/shell-descriptors")));

            var request = _mockRegistry.ReceivedRequests.First(r =>
                r.Method == "POST" && r.Path.StartsWith("/shell-descriptors"));
            var descriptor = JObject.Parse(request.Body!);

            Assert.Multiple(() =>
            {
                Assert.That(request.Path, Is.EqualTo("/shell-descriptors"), "request path");
                Assert.That(descriptor["id"]?.Value<string>(), Is.EqualTo(AasId), "id");
                Assert.That(descriptor["globalAssetId"]?.Value<string>(), Is.EqualTo(AssetId), "globalAssetId");
                Assert.That(descriptor["assetKind"]?.Value<string>(), Is.EqualTo("Instance"), "assetKind");
                Assert.That(descriptor["modelType"], Is.Null, "modelType should not be present");

                // Optional fields must be absent when not in source AAS body
                Assert.That(descriptor["idShort"], Is.Null, "idShort should be absent for minimal body");
                Assert.That(descriptor["description"], Is.Null, "description should be absent for minimal body");
                Assert.That(descriptor["displayName"], Is.Null, "displayName should be absent for minimal body");
                Assert.That(descriptor["administration"], Is.Null, "administration should be absent for minimal body");
                Assert.That(descriptor["specificAssetIds"], Is.Null, "specificAssetIds should be absent for minimal body");

                // Endpoint should always be present
                var endpoints = descriptor["endpoints"] as JArray;
                Assert.That(endpoints, Has.Count.EqualTo(1), "endpoints array");

                Assert.That(descriptor.Properties().Select(property => property.Name), Is.EquivalentTo(new[]
                {
                    "id",
                    "globalAssetId",
                    "assetKind",
                    "endpoints"
                }), "minimal descriptor should contain only the expected fields");
            });
        }

        [Test]
        public async Task Should_Not_Call_Registry_When_AAS_Body_Is_Missing_Id()
        {
            const string bodyMissingId = """
                {
                    "modelType": "AssetAdministrationShell",
                    "assetInformation": {
                        "globalAssetId": "urn:example:test-asset",
                        "assetKind": "Instance"
                    }
                }
                """;
            var content = new StringContent(bodyMissingId, Encoding.UTF8, "application/json");

            await _httpClientEnabled.PostAsync("/repo/shells", content);

            await Task.Delay(500);

            Assert.That(_mockRegistry.ReceivedRequests, Is.Empty);
        }

        [Test]
        public async Task Should_Not_Call_Registry_When_AAS_Body_Is_Missing_AssetInformation()
        {
            const string bodyMissingAssetInfo = """
                {
                    "modelType": "AssetAdministrationShell",
                    "id": "urn:example:test-aas"
                }
                """;
            var content = new StringContent(bodyMissingAssetInfo, Encoding.UTF8, "application/json");

            await _httpClientEnabled.PostAsync("/repo/shells", content);

            await Task.Delay(500);

            Assert.That(_mockRegistry.ReceivedRequests, Is.Empty);
        }

        [Test]
        public async Task Should_Not_Call_Registry_When_AAS_Body_Is_Missing_GlobalAssetId()
        {
            const string bodyMissingGlobalAssetId = """
                {
                    "modelType": "AssetAdministrationShell",
                    "id": "urn:example:test-aas",
                    "assetInformation": {
                        "assetKind": "Instance"
                    }
                }
                """;
            var content = new StringContent(bodyMissingGlobalAssetId, Encoding.UTF8, "application/json");

            await _httpClientEnabled.PostAsync("/repo/shells", content);

            await Task.Delay(500);

            Assert.That(_mockRegistry.ReceivedRequests, Is.Empty);
        }

        [Test]
        public async Task Should_Not_Call_Registry_When_Body_Is_Not_Valid_Json()
        {
            var content = new StringContent("not-valid-json{{{{", Encoding.UTF8, "application/json");

            await _httpClientEnabled.PostAsync("/repo/shells", content);

            await Task.Delay(500);

            Assert.That(_mockRegistry.ReceivedRequests, Is.Empty);
        }

        [Test]
        public async Task Proxy_Should_Return_Success_When_Registry_Returns_Server_Error()
        {
            _mockRegistry.ForcedStatusCode = StatusCodes.Status500InternalServerError;
            try
            {
                var content = new StringContent(ValidAasBody, Encoding.UTF8, "application/json");

                var response = await _httpClientEnabled.PostAsync("/repo/shells", content);

                // The proxy forwards to downstream (200 OK) regardless of registry outcome
                Assert.That(response.IsSuccessStatusCode, Is.True,
                    "Proxy should return a successful response even when the registry call fails");
                Assert.That(response.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.OK),
                    "Proxy should preserve the downstream success status");
                Assert.That(await response.Content.ReadAsStringAsync(), Is.EqualTo("AAS Repository Service called!"),
                    "Proxy should still return the downstream response body");

                // The middleware still attempted to reach the registry (fire-and-forget ran)
                await WaitForRegistryCallAsync(() => _mockRegistry.ReceivedRequests.Any(r =>
                    r.Method == "POST" && r.Path.StartsWith("/shell-descriptors")));

                Assert.That(_mockRegistry.ReceivedRequests, Has.Some.Matches<ReceivedRequest>(r =>
                    r.Method == "POST" && r.Path.StartsWith("/shell-descriptors")),
                    "Registry should have been called despite returning an error");
            }
            finally
            {
                _mockRegistry.ForcedStatusCode = null;
            }
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
