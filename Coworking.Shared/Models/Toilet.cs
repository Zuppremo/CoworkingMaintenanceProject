using Coworking.Shared.Enums;

namespace Coworking.Shared.Models;

public class Toilet : RestroomAsset
{
    public Toilet()
    {
        Type = AssetType.Toilet;
    }
}