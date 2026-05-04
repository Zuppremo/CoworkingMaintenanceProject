using Coworking.Shared.Enums;

namespace Coworking.Shared.Models;

public class Sink : RestroomAsset
{
    public Sink()
    {
        Type = AssetType.Sink;
    }
}