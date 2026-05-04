using System.Collections.Concurrent;

namespace CoworkingAPI.Features.BuildingTopology;

public class TopologyCache : ITopologyCache
{
    private readonly ConcurrentDictionary<Guid, byte> activeAssets = new ConcurrentDictionary<Guid, byte>();
    
    public bool IsValidAsset(Guid assetId)
    {
        return activeAssets.ContainsKey(assetId);
    }

    public void AddAsset(Guid assetId)
    {
        activeAssets.TryAdd(assetId, 1);
    }

    public void RemoveAsset(Guid assetId)
    {
        activeAssets.TryRemove(assetId, out _);
    }

    public void Seed(IEnumerable<Guid> assetIds)
    {
        activeAssets.Clear();
        foreach (Guid assetId in assetIds)
            AddAsset(assetId);
    }

    public void RefreshCache()
    {
    }
}