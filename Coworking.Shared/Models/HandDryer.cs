using Coworking.Shared.Enums;

namespace Coworking.Shared.Models;

public class HandDryer : RestroomAsset
{
    public bool isDamaged { get; set; }
    public HandDryer()
    {
        Type = AssetType.HandDryer;
    }
}