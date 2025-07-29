using mnestix_proxy.Tests.TestMockService;
using System.IO;
using System.Net;

namespace mnestix_proxy.Tests.RoutingTests
{
    [TestFixture]
    public class RoutingTests : IDisposable
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

        [Test]
        public async Task Should_Route_To_Mnestix_Api_Cluster_When_Correct_Path_Is_Called()
        {
            // Act
            var request = new HttpRequestMessage(new HttpMethod("GET"), "/api/test-endpoint");
            var response = await _httpClient.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            Assert.Multiple(() =>
            {
                // Assert
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
                Assert.That(content, Is.EqualTo("Mnestix Api called!"));
            });
        }

        [Test]
        public async Task Should_Route_To_AasRepoCluster_When_Repo_Path_Is_Called()
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "/repo/shells");
            var response = await _httpClient.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            Assert.Multiple(() =>
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
                Assert.That(content, Is.EqualTo("AAS Repository Service called!"));
            });
        }

        [Test]
        public async Task Should_Route_To_SubmodelRepoCluster_When_SubmodelRepo_Path_Is_Called()
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "/repo/submodels/sample");
            var response = await _httpClient.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            Assert.Multiple(() =>
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
                Assert.That(content, Is.EqualTo("Submodel Repository Service called!"));
            });
        }

        [Test]
        public async Task Should_Route_To_DiscoveryCluster_When_Discovery_Path_Is_Called()
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "/discovery/discovery-test-endpoint");
            var response = await _httpClient.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            Assert.Multiple(() =>
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
                Assert.That(content, Is.EqualTo("Discovery Service called!"));
            });
        }

        [Test]
        public async Task Should_Return_404_When_Route_Does_Not_Match()
        {
            var response = await _httpClient.GetAsync("/non-existent-route");
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        }


        [OneTimeTearDown]
        public void Dispose()
        {
            _httpClient.Dispose();
            _mockDownstream.Dispose();
        }
    }
}
