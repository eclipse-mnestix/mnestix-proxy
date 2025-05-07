using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Identity.Web;
using Microsoft.IdentityModel.Tokens;

namespace mnestix_proxy.Authentication;

/// <summary>
/// Provides extension methods for registering authentication services in an application.
/// </summary>
public static class AuthenticationServicesRegistration
{
    /// <summary>
    /// Configures and adds authentication services to the specified IServiceCollection. 
    /// This includes setting up OpenID Connect and Azure AD authentication based on the 
    /// provided configuration settings.
    /// </summary>
    /// <param name="services">The IServiceCollection to add the authentication services to.</param>
    /// <param name="configuration">The configuration settings for authentication.</param>
    public static void AddAuthenticationServices(this IServiceCollection services, IConfiguration configuration)
    {
        var openIdEnabled = configuration.GetSection("OpenId").GetValue("EnableOpenIdAuth", false);
        var azureAdEnabled = configuration.GetSection("AzureAd").GetValue("EnableAzureAdAuth", false);

        if (openIdEnabled)
        {
            var openIdSection = configuration.GetSection("OpenId");
            var issuer = openIdSection.GetValue<string>("Issuer");
            var clientId = openIdSection.GetValue<string>("ClientId");
            var requireHttps = openIdSection.GetValue<bool>("RequireHttpsMetadata");
            // Configuration for Mnestix-Api Authentication
            // This section sets up the authentication services for the Mnestix API,
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer(options =>
            {
                options.Authority = issuer;
                options.Audience = clientId;
                options.RequireHttpsMetadata = requireHttps;

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateAudience = true,
                    ValidateIssuer = true,
                    ValidateLifetime = true,
                    ValidAudience = clientId,
                    ValidIssuer = issuer,
                };
            });
        }
        else if (azureAdEnabled)
        {
            // Adds Microsoft Identity platform (AAD v2.0) support to protect this Api
            services.AddMicrosoftIdentityWebApiAuthentication(configuration);
        }
        else {
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer();
        }
    }
}