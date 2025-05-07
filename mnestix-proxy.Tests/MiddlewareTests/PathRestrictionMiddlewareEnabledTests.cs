using Microsoft.AspNetCore.Mvc.Testing;
using mnestix_proxy.Tests.TestMockService;
using System.Net;

namespace mnestix_proxy.Tests.MiddlewareTests
{
    [TestFixture]
    public class PathRestrictionMiddlewareEnabledTests : IDisposable
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
                { "Features:AllowRetrievingAllShellsAndSubmodels", "true" },
            });
            _httpClient = _factory.CreateClient();
        }

        [TestCase("/repo/shells")]
        [TestCase("/repo/submodels")]
        public async Task RestrictedPaths_Should_Return_200_When_Feature_Enabled(string path)
        {
            // Act
            var response = await _httpClient.GetAsync(path);
            var content = await response.Content.ReadAsStringAsync();

            Assert.Multiple(() =>
            {
                // Assert
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            });
        }

        [OneTimeTearDown]
        public void Dispose()
        {
            _httpClient.Dispose();
            _mockDownstream.Dispose();
        }
    }
}
