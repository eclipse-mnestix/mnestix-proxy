
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using mnestix_proxy.Middleware;

namespace mnestix_proxy.Tests
{
    public class IntegrationTestBase(string downstreamUrl) : WebApplicationFactory<Program>
    {
        private readonly string _downstreamUrl = downstreamUrl;

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureAppConfiguration((context, config) =>
            {
                var inMemorySettings = new Dictionary<string, string>
                {
                    { "CustomerEndpointsSecurity:ApiKey", "verySecureApiKeyMock" },
                    { "Features:AllowRetrievingAllShellsAndSubmodels", "false" },
                    { "ReverseProxy:Clusters:aasRepoCluster:Destinations:destination1:Address", _downstreamUrl },
                    { "ReverseProxy:Clusters:submodelRepoCluster:Destinations:destination1:Address", _downstreamUrl },
                };
                config.AddInMemoryCollection(inMemorySettings);
            });
        }
    }
}
