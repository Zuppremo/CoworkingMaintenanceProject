using Coworking.Shared.Enums;
using MessagePack;

namespace Coworking.Shared.Models.DTOs.Telemetry;

[MessagePackObject]
public record RestroomAssetDto
{
    [Key(0)]
    public Guid Id { get; init; }
    
    [Key(1)]
    public AssetType  Type { get; init; }
    
    [Key(2)]
    public short PositionX { get; init; }
    
    [Key(3)]
    public short PositionY { get; init; }
    
    [Key(4)]
    public bool HasActiveIssue { get; init; }
}