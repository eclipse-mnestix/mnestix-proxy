using Microsoft.AspNetCore.Mvc.Testing;
using mnestix_proxy.Tests.TestMockService;
using System.Net;

namespace mnestix_proxy.Tests.MiddlewareTests
{
    [TestFixture]
    public class PathRestrictionMiddlewareTests : IDisposable
    {
        private DownstreamService _mockDownstream;
        private HttpClient _httpClient;

        [OneTimeSetUp]
        public void Setup()
        {
            _mockDownstream = new DownstreamService();
            if (_mockDownstream.Url == null) return;
            var _factory = new IntegrationTestBase(_mockDownstream.Url, new Dictionary<string, string>
            {
                { "Features:AllowRetrievingAllShellsAndSubmodels", "false" },
            });
            _httpClient = _factory.CreateClient();
        }

        [TestCase("/repo/shells")]
        [TestCase("/repo/submodels")]
        [TestCase("/registry/shell-descriptors")]
        [TestCase("/registry/submodel-descriptors")]
        [TestCase("/repo/shells/")]
        [TestCase("/registry/shell-descriptors/")]
        [TestCase("/registry/submodel-descriptors/")]
        [TestCase("/REGISTRY/Shell-Descriptors")]
        [TestCase("/repo//shells")]
        [TestCase("/repo//submodels")]
        [TestCase("/repo///shells")]
        [TestCase("/repo/submodels/")]
        [TestCase("/repo/%2e/shells")]
        [TestCase("/repo/submodels/%2e%2e/shells")]
        [TestCase("/repo/%252e/shells")]
        [TestCase("/repo/submodels/%252e%252e/shells")]
        [TestCase("/repo/shell-descriptors")]
        [TestCase("/repo/submodel-descriptors")]
        public async Task Should_Return_405_When_Middleware_Feature_Disabled(string path)
        {
            // Act
            var response = await _httpClient.GetAsync(path);
            var content = await response.Content.ReadAsStringAsync();

            Assert.Multiple(() =>
            {
                // Assert
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.MethodNotAllowed));
                Assert.That(content, Is.EqualTo("Access to the requested path is restricted."));
            });
        }

        [TestCase("/repo/shells", "POST")]
        [TestCase("/repo/submodels/mockBase64EncodedSubmodelId", "GET")]
        [TestCase("/registry/shell-descriptors", "POST")]
        [TestCase("/registry/shell-descriptors/mockBase64EncodedAasId", "GET")]
        [TestCase("/registry/submodel-descriptors/mockBase64EncodedSubmodelId", "GET")]
        [TestCase("/repo/submodels/abc//def", "GET")]
        [TestCase("/repo/shells/aGVsbG8/", "GET")]
        public async Task Should_Forward_Request_When_Middleware_Feature_Disabled(string path, string method)
        {
            // Act
            var request = new HttpRequestMessage(new HttpMethod(method), path);
            // Everything except GET/HEAD/OPTIONS needs the API key, otherwise the request is
            // rejected with 401 before it reaches the proxy pipeline and proves nothing.
            request.Headers.Add("X-API-KEY", "verySecureApiKeyMock");
            var response = await _httpClient.SendAsync(request);

            // Assert
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        }

        [TestCase("/repo/shells")]
        [TestCase("/repo//shells")]
        public async Task Should_Return_405_For_Head_Requests_When_Middleware_Feature_Disabled(string path)
        {
            // Act
            var request = new HttpRequestMessage(HttpMethod.Head, path);
            request.Headers.Add("X-API-KEY", "verySecureApiKeyMock");
            var response = await _httpClient.SendAsync(request);

            // Assert
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.MethodNotAllowed));
        }

        [OneTimeTearDown]
        public void Dispose()
        {
            _httpClient.Dispose();
            _mockDownstream.Dispose();
        }
    }
}
