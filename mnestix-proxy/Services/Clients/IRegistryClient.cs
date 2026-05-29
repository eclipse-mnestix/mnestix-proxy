namespace mnestix_proxy.Services.Clients
{
    public interface IRegistryClient
    {
        Task<(bool isSuccess, string Result)> RegisterOrUpdateShellDescriptor(string aasId, string globalAssetId);
        Task<(bool isSuccess, string Result)> DeleteShellDescriptor(string aasIdentifier);
    }
}
