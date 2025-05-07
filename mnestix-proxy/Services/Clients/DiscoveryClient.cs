using Microsoft.Extensions.Options;
using mnestix_proxy.Configuration;
using mnestix_proxy.Exceptions;
using mnestix_proxy.Services.Shared;
using Newtonsoft.Json.Linq;
using RestSharp;
using System.Net;

namespace mnestix_proxy.Services.Clients
{
    public class DiscoveryClient(IOptions<DiscoveryServiceOptions> options) : IDiscoveryClient
    {
        public Task<(bool isSuccess, List<string> Result)> GetAasIdsByAssetId(string assetId)
        {
            var assetIds = new List<Tuple<string, string>>
        {
            Tuple.Create("globalAssetId", assetId),
        };
            return GetAllAssetAdministrationShellIdsByAssetLink(assetIds);
        }

        public async Task<(bool isSuccess, List<string> Result)> GetAllAssetAdministrationShellIdsByAssetLink(List<Tuple<string, string>> assetIds)
        {
            var client = new RestClient(options.Value.Address);

            var assetIdsQuery = new List<string>();

            foreach (var tuple in assetIds)
            {
                var jsonContent = new JObject
            {
                { "name", tuple.Item1 },
                { "value", tuple.Item2 }
            };
                assetIdsQuery.Add(Base64StringDeAndEncoder.EncodeTo64(jsonContent.ToString()));
            }

            var request = new RestRequest("/lookup/shells");
            foreach (var assetIdEnc in assetIdsQuery)
            {
                request.AddQueryParameter("assetIds", assetIdEnc);
            }
            var response = await client.GetAsync(request);

            if (response.IsSuccessful == false || response.Content == null)
            {
                return new ValueTuple<bool, List<string>>(false,
                    [response.ErrorMessage ?? "Could not get from repository."]);
            }
            var result = JObject.Parse(response.Content).GetValue("result")!.Values<string>().ToList();

            return new ValueTuple<bool, List<string>>(true, result!);
        }

        public async Task<(bool isSuccess, List<string> Result)> GetAllAssetLinksById(string aasIdentifier)
        {
            var client = new RestClient(options.Value.Address);
            var b64AasId = Base64StringDeAndEncoder.EncodeTo64(aasIdentifier);
            var request = new RestRequest("/lookup/shells/" + b64AasId);
            var response = await client.GetAsync(request);
            if (response.IsSuccessful == false || response.Content == null || !JArray.Parse(response.Content).HasValues)
            {
                return new ValueTuple<bool, List<string>>(false,
                    [response.ErrorMessage ?? "Could not get from repository."]);
            }

            var result = JArray.Parse(response.Content).Select(x => new
            {
                name = (string)x["name"],
                value = (string)x["value"]
            }.ToString()).ToList();

            return new ValueTuple<bool, List<string>>(true, result!);
        }

        public Task<(bool isSuccess, string Result)> LinkAasIdAndAssetId(string aasId, string assetId)
        {
            var assetIds = new List<Tuple<string, string>>
        {
            Tuple.Create("globalAssetId", assetId),
        };
            return PostAllAssetLinksById(aasId, assetIds);
        }

        public async Task<(bool isSuccess, string Result)> PostAllAssetLinksById(string aasIdentifier, List<Tuple<string, string>> assetLinks)
        {
            var client = new RestClient(options.Value.Address);
            var b64AasId = Base64StringDeAndEncoder.EncodeTo64(aasIdentifier);
            var restRequest = new RestRequest("/lookup/shells/" + b64AasId)
            {
                RequestFormat = DataFormat.Json,
                Method = Method.Post
            };

            var assetIdsJson = new JArray();

            foreach (var tuple in assetLinks)
            {
                var assetLinkJson = new JObject
            {
                { "name", tuple.Item1 },
                { "value", tuple.Item2}
            };

                assetIdsJson.Add(assetLinkJson);
            }

            restRequest.AddBody(assetIdsJson.ToString(), "application/json");

            var response = await client.PostAsync(restRequest);

            if (response.StatusCode != HttpStatusCode.OK && response.StatusCode != HttpStatusCode.Created)
            {
                return new ValueTuple<bool, string>(false,
                    $"Could not post: {response.Content} Code: {response.StatusCode}" + response.ErrorException);
            }

            return new ValueTuple<bool, string>(true, response.Content!);
        }

        public async Task<(bool isSuccess, string Result)> DeleteAllAssetLinksById(string aasIdentifier)
        {
            var client = new RestClient(options.Value.Address);
            var b64AasId = Base64StringDeAndEncoder.EncodeTo64(aasIdentifier);
            var restRequest = new RestRequest("/" + "lookup/shells/" + b64AasId)
            {
                RequestFormat = DataFormat.Json,
                Method = Method.Delete
            };

            var response = await client.DeleteAsync(restRequest);

            if (response.StatusCode != HttpStatusCode.NoContent && response.StatusCode != HttpStatusCode.OK)
            {
                throw new RepoProxyException(
                    ErrorCodes.CouldNotPostShell,
                    $"Could not post: {response.Content} Code: {response.StatusCode}",
                    response.ErrorException);
            }

            return new ValueTuple<bool, string>(true, response.Content!);
        }
    }
}
