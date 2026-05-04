using Coworking.Shared.Enums;

namespace Coworking.Shared.Extensions;

public static class AssetTypeExtensions
{
    public static MountingLayer GetMountingLayer(this AssetType assetType) => assetType switch
    {
        AssetType.Toilet => MountingLayer.Floor,
        AssetType.Sink => MountingLayer.Floor,
        AssetType.HandDryer => MountingLayer.Wall,
        AssetType.PaperDispenser => MountingLayer.Wall,
        _ => MountingLayer.Floor // Default safe fallback
    };
}