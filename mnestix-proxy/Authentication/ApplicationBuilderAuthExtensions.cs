using Microsoft.Extensions.Configuration;

namespace mnestix_proxy.Authentication
{
    public static class ApplicationBuilderAuthExtensions
    {
        public static IApplicationBuilder UseMnestixConfiguredAuth(this IApplicationBuilder app, IConfiguration configuration)
        {
            var openIdEnabled = configuration.GetSection("OpenId").GetValue("EnableOpenIdAuth", false);
            var azureAdEnabled = configuration.GetSection("AzureAd").GetValue("EnableAzureAdAuth", false);

            if (openIdEnabled || azureAdEnabled) {
                app.UseAuthentication();
                app.UseAuthorization();
            }

            return app;
        }
    }
}
