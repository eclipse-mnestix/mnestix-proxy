namespace mnestix_proxy.Services.Clients
{
    public interface IDiscoveryClient
    {
        // The two functions below just are convenience functions to abstract some complexity away
        Task<(bool isSuccess, string Result)> LinkAasIdAndAssetId(string aasId, string assetId);
        Task<(bool isSuccess, string Result)> DeleteAllAssetLinksById(string aasIdentifier);

        // The functions below are the ones, that were introduced in
        // Specification of the Asset Administration Shell Part 2: Application Programming Interfaces
        Task<(bool isSuccess, List<string> Result)> GetAasIdsByAssetId(string assetId);
        Task<(bool isSuccess, List<string> Result)> GetAllAssetAdministrationShellIdsByAssetLink(
            List<Tuple<string, string>> assetIds);
        Task<(bool isSuccess, List<string> Result)> GetAllAssetLinksById(string aasIdentifier);
        Task<(bool isSuccess, string Result)> PostAllAssetLinksById(string aasIdentifier, List<Tuple<string, string>> assetLinks);
    }
}
