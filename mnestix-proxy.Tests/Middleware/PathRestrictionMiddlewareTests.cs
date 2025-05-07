using Microsoft.AspNetCore.Mvc.Testing;
using mnestix_proxy.Tests.TestMockService;
using System.Net;

namespace mnestix_proxy.Tests.Middleware
{
    [TestFixture]
    public class PathRestrictionMiddlewareTests : IDisposable
    {
        private DownstreamTestService _mockDownstream;
        private HttpClient _httpClient;

        [SetUp]
        public void Setup()
        {
            _mockDownstream = new DownstreamTestService();
            var _factory = new IntegrationTestBase(_mockDownstream.Url);
            _httpClient = _factory.CreateClient();
        }

        [TestCase("/repo/shells")]
        [TestCase("/repo/submodels")]
        public async Task RestrictedPaths_Should_Return_405_When_Feature_Disabled(string path)
        {
            // Act
            var response = await _httpClient.GetAsync(path);
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.AreEqual(HttpStatusCode.MethodNotAllowed, response.StatusCode);
            Assert.AreEqual("Access to the requested path is restricted.", content);
        }

        [TestCase("/repo/shells", "POST")]
        [TestCase("/repo/submodels/mockBase64EncodedSubmodelId", "GET")]
        public async Task NonRestrictedRequests_Should_Not_Return_405(string path, string method)
        {
            // Act
            var request = new HttpRequestMessage(new HttpMethod(method), path);
            var response = await _httpClient.SendAsync(request);

            // Assert
            Assert.AreNotEqual(HttpStatusCode.MethodNotAllowed, response.StatusCode);
        }

        [TearDown]
        public void Dispose()
        {
            _httpClient.Dispose();
            _mockDownstream.Dispose();
        }
    }
}
