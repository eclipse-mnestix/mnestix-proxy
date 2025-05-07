
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using mnestix_proxy.Middleware;

namespace mnestix_proxy.Tests
{
    public class IntegrationTestBase(string downstreamUrl, IDictionary<string, string>? customSettings = null) : WebApplicationFactory<Program>
    {
        private readonly string _downstreamUrl = downstreamUrl;
        private readonly IDictionary<string, string>? _customSettings = customSettings;

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureAppConfiguration((context, config) =>
            {
                var inMemorySettings = new Dictionary<string, string>
                {
                    { "CustomerEndpointsSecurity:ApiKey", "verySecureApiKeyMock" },
                    { "Features:AllowRetrievingAllShellsAndSubmodels", "true" },
                    { "ReverseProxy:Clusters:aasRepoCluster:Destinations:destination1:Address", _downstreamUrl },
                    { "ReverseProxy:Clusters:submodelRepoCluster:Destinations:destination1:Address", _downstreamUrl },
                };

                if (_customSettings != null)
                {
                    foreach (var kvp in _customSettings)
                    {
                        inMemorySettings[kvp.Key] = kvp.Value;
                    }
                }

                config.AddInMemoryCollection(inMemorySettings);
            });
        }
    }
}
