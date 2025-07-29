using mnestix_proxy.Services.Clients;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using System.Text;

namespace mnestix_proxy.Middleware
{
    /// <summary>
    /// This middleware class is responsible for storing aas in the discovery 
    /// </summary>
    public static class AasDiscoveryServiceMiddleware
    {
        internal static Func<HttpContext, Func<Task>, Task> ConfigureAasDiscoveryHandling()
        {
            return (context, next) =>
            {
                var discoveryClient = context.RequestServices.GetService<IDiscoveryClient>();
                switch (context.Request.Method)
                {
                    case "PUT" or "POST" when context.Request.Path.HasValue &&
                                              context.Request.Path.StartsWithSegments("/repo"):
                        HandlePutToRepo(context, discoveryClient);
                        break;
                    case "DELETE" when context.Request.Path.StartsWithSegments("/repo") &&
                                       context.Request.Path.StartsWithSegments("/repo"):
                        HandleDeleteFromRepo(context, discoveryClient);
                        break;
                }

                return next();
            };
        }

        private static void HandlePutToRepo(HttpContext context, IDiscoveryClient discoveryClient)
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
                    // if the request cannot be parsed it might be a single value for an submodel element which the repo will handle correctly.
                }

                var modelType = requestBody["modelType"]?.Value<string>();
                if (modelType is "AssetAdministrationShell")
                {
                    var assetId = requestBody["assetInformation"]?["globalAssetId"]?.Value<string>();
                    var aasId = requestBody["id"]?.Value<string>();

                    Debug.Assert(aasId != null, nameof(aasId) + " != null");
                    Debug.Assert(assetId != null, nameof(assetId) + " != null");

                    discoveryClient.LinkAasIdAndAssetId(aasId: aasId, assetId: assetId);
                }
            }

            // Rewind, so the core is not lost when it looks the body for the request
            context.Request.Body.Position = 0;
        }

        private static async void HandleDeleteFromRepo(HttpContext context, IDiscoveryClient discoveryClient)
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
                    // if the request cannot be parsed it might be a single value for an submodel element which the repo will handle correctly.
                }

                var modelType = requestBody["modelType"]?.Value<string>();
                if (modelType is "AssetAdministrationShell")
                {
                    var aasId = requestBody["id"]?.Value<string>();

                    Debug.Assert(aasId != null, nameof(aasId) + " != null");

                    await discoveryClient.DeleteAllAssetLinksById(aasIdentifier: aasId);
                }
            }

            // Rewind, so the core is not lost when it looks the body for the request
            context.Request.Body.Position = 0;
        }
    }
}
