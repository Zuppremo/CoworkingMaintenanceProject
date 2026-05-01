using Coworking.Shared.Enums;

namespace Coworking.Shared.Models.DTOs.Building;

public class RestroomAssetDto
{
    public Guid Id { get; set; }
    public AssetType  Type { get; set; }
    
    public int PositionX { get; set; }
    public int PositionY { get; set; }
    
    public bool HasActiveIssue { get; set; }
}