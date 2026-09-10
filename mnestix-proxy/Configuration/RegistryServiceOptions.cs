namespace mnestix_proxy.Configuration
{
    public class RegistryServiceOptions
    {
        /// <summary>
        /// Name of the configuration section in appsettings.json
        /// </summary>
        public const string Options = "ReverseProxy:Clusters:aasRegistryCluster:Destinations:destination1";

        /// <summary>
        /// The base address of the AAS Registry service.
        /// </summary>
        public string Address { get; set; } = string.Empty;
    }
}
