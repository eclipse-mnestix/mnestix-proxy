using mnestix_proxy.Tests.TestMockService;
using System.IO;
using System.Net;
using System.Reflection.Metadata;

namespace mnestix_proxy.Tests.AuthenticationTests
{
    [TestFixture]
    public class ApiKeyRequirementHandlerTests
    {
        private const string ValidApiKey = "verySecureApiKeyMock";
        private DownstreamService _mockDownstream;
        private HttpClient _httpClient;

        [OneTimeSetUp]
        public void Setup()
        {
            _mockDownstream = new DownstreamService();
            if (_mockDownstream.Url == null) return;
            var _factory = new IntegrationTestBase(_mockDownstream.Url);
            _httpClient = _factory.CreateClient();
        }

        [TestCase("/repo/shells", "GET")]
        [TestCase("/repo/shells", "HEAD")]
        [TestCase("/repo/shells/mockBase64EncodedAasId", "GET")]
        [TestCase("/repo/submodels", "GET")]
        [TestCase("/repo/submodels/mockBase64EncodedSubmodelId", "GET")]
        public async Task Should_Succeed_For_GET_and_HEAD_Requests_Without_ApiKey(string path, string method)
        {
            // Act
            var request = new HttpRequestMessage(new HttpMethod(method), path);
            var response = await _httpClient.SendAsync(request);

            // Assert
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        }

        [TestCase("/repo/shells", "POST")]
        [TestCase("/repo/shells", "PUT")]
        [TestCase("/repo/shells/mockBase64EncodedAasId", "DELETE")]
        [TestCase("/repo/submodels", "POST")]
        [TestCase("/repo/submodels/mockBase64EncodedSubmodelId", "PUT")]
        public async Task Should_Succeed_For_POST_PUT_DELETE_Requests_With_Correct_ApiKey(string path, string method)
        {
            // Act
            var request = new HttpRequestMessage(new HttpMethod(method), path);
            request.Headers.Add("X-API-KEY", ValidApiKey);
            var response = await _httpClient.SendAsync(request);

            // Assert
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        }

        [TestCase("/repo/shells", "POST")]
        [TestCase("/repo/shells", "PUT")]
        [TestCase("/repo/shells/mockBase64EncodedAasId", "DELETE")]
        [TestCase("/repo/submodels", "POST")]
        [TestCase("/repo/submodels/mockBase64EncodedSubmodelId", "PUT")]
        public async Task Should_Fail_For_POST_PUT_DELETE_Requests_Without_ApiKey(string path, string method)
        {
            // Act
            var request = new HttpRequestMessage(new HttpMethod(method), path);
            var response = await _httpClient.SendAsync(request);

            // Assert
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
        }

        [TestCase("/repo/shells", "POST")]
        [TestCase("/repo/shells", "PUT")]
        [TestCase("/repo/shells/mockBase64EncodedAasId", "DELETE")]
        [TestCase("/repo/submodels", "POST")]
        [TestCase("/repo/submodels/mockBase64EncodedSubmodelId", "PUT")]
        public async Task Should_Fail_For_POST_PUT_DELETE_Requests_With_Incorrect_ApiKey(string path, string method)
        {
            // Act
            var request = new HttpRequestMessage(new HttpMethod(method), path);
            request.Headers.Add("X-API-KEY", "IncorrectApiKey");
            var response = await _httpClient.SendAsync(request);

            // Assert
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
        }

        [OneTimeTearDown]
        public void Dispose()
        {
            _httpClient.Dispose();
            _mockDownstream.Dispose();
        }
    }
}
