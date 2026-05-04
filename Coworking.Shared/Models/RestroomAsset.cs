using Coworking.Shared.Enums;
using Coworking.Shared.Models.Interfaces;

namespace Coworking.Shared.Models;

public abstract class RestroomAsset : ISoftDeletable
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public AssetType  Type { get; set; }
    
    public short PositionX { get; set; }
    public short PositionY { get; set; }
    
    public Guid RestroomId { get; set; }
    public bool IsDamaged { get; set; }
    public bool IsDeleted { get; set; }
}