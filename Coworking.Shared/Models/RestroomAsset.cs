using Coworking.Shared.Enums;

namespace Coworking.Shared.Models;

public abstract class RestroomAsset
{
    public Guid Id { get; set; } = new Guid();
    public AssetType  Type { get; set; }
    
    public int PositionX { get; set; }
    public int PositionY { get; set; }
    
    public Guid RestroomId { get; set; }
}