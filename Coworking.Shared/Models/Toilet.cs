using Coworking.Shared.Enums;

namespace Coworking.Shared.Models;

public class Toilet : RestroomAsset
{
    public bool isDamaged { get; set; }
    
    public Toilet()
    {
        Type = AssetType.Toilet;
    }
}