using Coworking.Shared.Enums;

namespace Coworking.Shared.Models;

public class HandDryer : RestroomAsset
{
    public HandDryer()
    {
        Type = AssetType.HandDryer;
    }
}