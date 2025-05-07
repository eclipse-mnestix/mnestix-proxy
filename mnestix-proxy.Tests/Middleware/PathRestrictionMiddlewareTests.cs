using Microsoft.AspNetCore.Mvc.Testing;
using mnestix_proxy.Tests.TestMockService;
using System.Net;

namespace mnestix_proxy.Tests.Middleware
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

            Assert.Multiple(() =>
            {
                // Assert
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.MethodNotAllowed));
                Assert.That(content, Is.EqualTo("Access to the requested path is restricted."));
            });
        }

        [TestCase("/repo/shells", "POST")]
        [TestCase("/repo/submodels/mockBase64EncodedSubmodelId", "GET")]
        public async Task NonRestrictedRequests_Should_Not_Return_405(string path, string method)
        {
            // Act
            var request = new HttpRequestMessage(new HttpMethod(method), path);
            var response = await _httpClient.SendAsync(request);

            // Assert
            Assert.That(response.StatusCode, Is.Not.EqualTo(HttpStatusCode.MethodNotAllowed));
        }

        [OneTimeTearDown]
        public void Dispose()
        {
            _httpClient.Dispose();
            _mockDownstream.Dispose();
        }
    }
}
