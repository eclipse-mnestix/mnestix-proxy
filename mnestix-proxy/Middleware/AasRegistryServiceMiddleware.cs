using mnestix_proxy.Services.Clients;
using mnestix_proxy.Services.Shared;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text;

namespace mnestix_proxy.Middleware
{
    /// <summary>
    /// This middleware class is responsible for registering and deregistering AAS shell descriptors
    /// in the AAS Registry whenever shells are created, updated, or deleted via the repository.
    /// </summary>
    public static class AasRegistryServiceMiddleware
    {
        internal static Func<HttpContext, Func<Task>, Task> ConfigureAasRegistryHandling()
        {
            return (context, next) =>
            {
                var registryClient = context.RequestServices.GetRequiredService<IRegistryClient>();
                var logger = context.RequestServices.GetRequiredService<ILoggerFactory>()
                    .CreateLogger(nameof(AasRegistryServiceMiddleware));

                switch (context.Request.Method)
                {
                    case "PUT" or "POST" when context.Request.Path.HasValue &&
                                              context.Request.Path.StartsWithSegments("/repo"):
                        HandlePutToRepo(context, registryClient, logger);
                        break;
                    case "DELETE" when context.Request.Path.HasValue &&
                                       context.Request.Path.StartsWithSegments("/repo"):
                        _ = HandleDeleteFromRepoAsync(context, registryClient, logger);
                        break;
                }

                return next();
            };
        }

        private static void HandlePutToRepo(HttpContext context, IRegistryClient registryClient, ILogger logger)
        {
            context.Request.EnableBuffering();
            using (var reader
                   = new StreamReader(context.Request.Body, Encoding.UTF8, true, 1024, true))
            {
                var requestBody = new JObject();
                try
                {
                    var bodyStr = reader.ReadToEndAsync();
                    requestBody = JObject.Parse(bodyStr.Result);
                }
                catch (JsonReaderException)
                {
                    // we do not want to break the request here.
                    // if the request cannot be parsed it might be a single value for a submodel element which the repo will handle correctly.
                }

                var modelType = requestBody["modelType"]?.Value<string>();
                if (modelType is "AssetAdministrationShell")
                {
                    var assetId = requestBody["assetInformation"]?["globalAssetId"]?.Value<string>();
                    var aasId = requestBody["id"]?.Value<string>();

                    if (aasId != null && assetId != null)
                    {
                        var baseUrl = $"{context.Request.Scheme}://{context.Request.Host}";
                        var descriptor = BuildShellDescriptor(requestBody, aasId, baseUrl);

                        _ = registryClient.RegisterOrUpdateShellDescriptor(aasId: aasId, shellDescriptorJson: descriptor.ToString())
                            .ContinueWith(t =>
                            {
                                if (t.IsFaulted)
                                    logger.LogError(t.Exception, "Unexpected error registering AAS {AasId} in registry.", aasId);
                                else if (!t.Result.isSuccess)
                                    logger.LogWarning("Failed to register AAS {AasId} in registry: {Result}", aasId, t.Result.Result);
                            }, TaskScheduler.Default);
                    }
                }
            }

            // Rewind so the core request body is not lost when the proxy forwards the request
            context.Request.Body.Position = 0;
        }

        private static async Task HandleDeleteFromRepoAsync(HttpContext context, IRegistryClient registryClient, ILogger logger)
        {
            // DELETE /repo/shells/{base64AasId} — the AAS ID is encoded in the URL path
            var segments = context.Request.Path.Value?.Split('/') ?? [];

            // Expected path segments: ["", "repo", "shells", "{base64AasId}"]
            if (segments.Length >= 4 && segments[2].Equals("shells", StringComparison.OrdinalIgnoreCase))
            {
                var b64AasId = segments[3];
                if (!string.IsNullOrEmpty(b64AasId))
                {
                    try
                    {
                        var aasId = Base64StringDeAndEncoder.DecodeFrom64(b64AasId);
                        var (isSuccess, result) = await registryClient.DeleteShellDescriptor(aasIdentifier: aasId);
                        if (!isSuccess)
                            logger.LogWarning("Failed to delete shell descriptor for AAS {AasId}: {Result}", aasId, result);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Unexpected error deleting shell descriptor for AAS ID segment {B64AasId}.", b64AasId);
                    }
                }
            }
        }

        /// <summary>
        /// Converts an AssetAdministrationShell body (AAS Repository format) into an
        /// AssetAdministrationShellDescriptor (AAS Registry format), mapping all available
        /// fields and appending an endpoint pointing back to this proxy.
        /// </summary>
        private static JObject BuildShellDescriptor(JObject aasBody, string aasId, string proxyBaseUrl)
        {
            var assetInfo = aasBody["assetInformation"];

            var descriptor = new JObject
            {
                ["id"] = aasId,
                ["globalAssetId"] = assetInfo?["globalAssetId"]
            };

            // Pass-through optional metadata fields
            if (aasBody["idShort"] is { } idShort)
                descriptor["idShort"] = idShort;
            if (aasBody["description"] is { } description)
                descriptor["description"] = description;
            if (aasBody["displayName"] is { } displayName)
                descriptor["displayName"] = displayName;
            if (aasBody["administration"] is { } administration)
                descriptor["administration"] = administration;

            // Flatten assetInformation fields to the descriptor top level
            if (assetInfo?["assetKind"] is { } assetKind)
                descriptor["assetKind"] = assetKind;
            if (assetInfo?["assetType"] is { } assetType)
                descriptor["assetType"] = assetType;
            if (assetInfo?["specificAssetIds"] is { } specificAssetIds)
                descriptor["specificAssetIds"] = specificAssetIds;

            // Endpoint pointing to this proxy's repo path for the AAS
            var href = $"{proxyBaseUrl}/repo/shells/{Base64StringDeAndEncoder.EncodeTo64(aasId)}";
            descriptor["endpoints"] = new JArray
            {
                new JObject
                {
                    ["protocolInformation"] = new JObject
                    {
                        ["href"] = href,
                        ["endpointProtocol"] = "HTTP",
                        ["endpointProtocolVersion"] = new JArray { "1.1" }
                    },
                    ["interface"] = "AAS-3.0"
                }
            };

            return descriptor;
        }
    }
}
