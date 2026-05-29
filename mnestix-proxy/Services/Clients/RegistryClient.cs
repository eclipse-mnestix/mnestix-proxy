using Microsoft.Extensions.Options;
using mnestix_proxy.Configuration;
using mnestix_proxy.Services.Shared;
using RestSharp;
using System.Net;

namespace mnestix_proxy.Services.Clients
{
    public class RegistryClient(IOptions<RegistryServiceOptions> options) : IRegistryClient
    {
        public async Task<(bool isSuccess, string Result)> RegisterOrUpdateShellDescriptor(string aasId, string shellDescriptorJson)
        {
            var client = new RestClient(options.Value.Address);
            var b64AasId = Base64StringDeAndEncoder.EncodeTo64(aasId);

            // Try POST first (create new descriptor)
            var postRequest = new RestRequest("/shell-descriptors")
            {
                RequestFormat = DataFormat.Json,
                Method = Method.Post
            };
            postRequest.AddBody(shellDescriptorJson, "application/json");

            var postResponse = await client.PostAsync(postRequest);

            if (postResponse.StatusCode is HttpStatusCode.Created or HttpStatusCode.OK)
            {
                return (true, postResponse.Content ?? string.Empty);
            }

            // If conflict (409), the descriptor already exists — update it via PUT
            if (postResponse.StatusCode == HttpStatusCode.Conflict)
            {
                var putRequest = new RestRequest("/shell-descriptors/" + b64AasId)
                {
                    RequestFormat = DataFormat.Json,
                    Method = Method.Put
                };
                putRequest.AddBody(shellDescriptorJson, "application/json");

                var putResponse = await client.PutAsync(putRequest);

                if (putResponse.StatusCode is HttpStatusCode.OK or HttpStatusCode.NoContent)
                {
                    return (true, putResponse.Content ?? string.Empty);
                }

                return (false, $"Could not update shell descriptor: {putResponse.Content} Code: {putResponse.StatusCode}");
            }

            return (false, $"Could not register shell descriptor: {postResponse.Content} Code: {postResponse.StatusCode}");
        }

        public async Task<(bool isSuccess, string Result)> DeleteShellDescriptor(string aasIdentifier)
        {
            var client = new RestClient(options.Value.Address);
            var b64AasId = Base64StringDeAndEncoder.EncodeTo64(aasIdentifier);
            var request = new RestRequest("/shell-descriptors/" + b64AasId)
            {
                RequestFormat = DataFormat.Json,
                Method = Method.Delete
            };

            var response = await client.DeleteAsync(request);

            if (response.StatusCode is not (HttpStatusCode.NoContent or HttpStatusCode.OK))
            {
                return (false, $"Could not delete shell descriptor: {response.Content} Code: {response.StatusCode}");
            }

            return (true, response.Content ?? string.Empty);
        }
    }
}
