using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using mnestix_proxy.Authentication;
using mnestix_proxy.Authentication.ApiKeyAuthentication;
using mnestix_proxy.Authentication.ApiKeyAuthorization;
using mnestix_proxy.Configuration;
using mnestix_proxy.Middleware;
using mnestix_proxy.Services.Clients;

namespace mnestix_proxy
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.Services.AddReverseProxy()
                .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

            //Discovery Client settings
            builder.Services.AddTransient<IDiscoveryClient, DiscoveryClient>();
            builder.Services.Configure<DiscoveryServiceOptions>(
                builder.Configuration.GetSection(DiscoveryServiceOptions.Options));

            // Registry Client settings
            builder.Services.AddTransient<IRegistryClient, RegistryClient>();
            builder.Services.Configure<RegistryServiceOptions>(
                builder.Configuration.GetSection(RegistryServiceOptions.Options));

            builder.Services.AddAuthenticationServices(builder.Configuration);

            // Adds authorization handler
            builder.Services.AddScoped<IAuthorizationHandler, ApiKeyRequirementHandler>();

            builder.Services.AddOptions<CustomerEndpointsSecurityOptions>()
                .Bind(builder.Configuration.GetSection(CustomerEndpointsSecurityOptions.CustomerEndpointsSecurity))
                .ValidateOnStart();
            builder.Services.AddSingleton<IValidateOptions<CustomerEndpointsSecurityOptions>,
                CustomerEndpointsSecurityOptionsValidation>();

            builder.Services.AddAuthorizationBuilder()
                .AddPolicy("customApiKeyToModifyValuesPolicy", policyBuilder => policyBuilder
                    .AddRequirements(new ApiKeyRequirement()));

            // in some classes we need the base url of the request
            builder.Services.AddHttpContextAccessor();

            builder.Services.AddCors(options =>
            {
                options.AddPolicy("allowAnything", corsPolicyOptions =>
                {
                    corsPolicyOptions.AllowAnyOrigin();
                    corsPolicyOptions.AllowAnyHeader();
                    corsPolicyOptions.AllowAnyMethod();
                });
            });

            // pipeline settings
            var app = builder.Build();

            // IMPORTANT: apply CORS BEFORE authentication so preflight OPTIONS are handled
            app.UseCors("allowAnything");

            app.UseMnestixConfiguredAuth(builder.Configuration);

            app.MapReverseProxy(proxyPipeline =>
            {
                // Path Restricting
                _ = bool.TryParse(builder.Configuration["Features:AllowRetrievingAllShellsAndSubmodels"],
                       out var allowRetrievingAllShellsAndSubmodels);
                if (!allowRetrievingAllShellsAndSubmodels)
                {
                    proxyPipeline.Use(PathRestrictionMiddleware.PathRestrictionHandling());
                }

                // AAS Discovery
                _ = bool.TryParse(builder.Configuration["Features:AasDiscoveryMiddleware"],
                    out var aasDiscoveryMiddleware);
                if (aasDiscoveryMiddleware)
                {
                    proxyPipeline.Use(AasDiscoveryServiceMiddleware.ConfigureAasDiscoveryHandling());
                }

                // AAS Registry
                _ = bool.TryParse(builder.Configuration["Features:AasRegistryMiddleware"],
                    out var aasRegistryMiddleware);
                if (aasRegistryMiddleware)
                {
                    proxyPipeline.Use(AasRegistryServiceMiddleware.ConfigureAasRegistryHandling());
                }

                // MQTT Eventing
                _ = bool.TryParse(builder.Configuration["MQTTEventing:mqtt_events_enabled"],
                    out var mqttEventingEnabled);
                if (mqttEventingEnabled)
                {
                    var logger = LoggerFactory.Create(builder => builder.AddConsole())
                        .CreateLogger<MqttEventingMiddleware>();
                    var mqttEventingMiddleware =
                        new MqttEventingMiddleware(builder.Configuration.GetSection("MQTTEventing"), logger);
                    proxyPipeline.Use(mqttEventingMiddleware.ConfigureMqttEventingHandling());
                }

            });

            app.Run();
        }
    }
}
