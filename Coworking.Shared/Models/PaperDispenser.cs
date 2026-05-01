using Coworking.Shared.Enums;

namespace Coworking.Shared.Models;

public class PaperDispenser : RestroomAsset
{
    public int PaperLevelPercentage { get; set; }
    
    public PaperDispenser()
    {
        Type = AssetType.PaperDispenser;
    }
}