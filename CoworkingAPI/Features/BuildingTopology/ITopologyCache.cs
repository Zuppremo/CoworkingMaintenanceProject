namespace CoworkingAPI.Features.BuildingTopology;

public interface ITopologyCache
{
    bool IsValidAsset(Guid assetId);
    void RefreshCache();
    void Seed(IEnumerable<Guid> assetIds);

    void AddAsset(Guid id);
    void RemoveAsset(Guid id);
}