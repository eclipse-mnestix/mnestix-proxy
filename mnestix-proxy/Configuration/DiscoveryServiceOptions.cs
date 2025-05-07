namespace mnestix_proxy.Configuration
{
    public class DiscoveryServiceOptions
    {
        /// <summary>
        /// Name of the configuration section in appsettings.json
        /// </summary>
        public const string Options = "ReverseProxy:Clusters:discoveryCluster:Destinations:destination1";

        /// <summary>
        /// The api key needed to request the endpoints.
        /// </summary>
        public string Address { get; set; } = string.Empty;
    }
}
