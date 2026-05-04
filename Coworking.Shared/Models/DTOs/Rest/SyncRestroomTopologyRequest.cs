using System.ComponentModel.DataAnnotations;
using Coworking.Shared.Enums;

namespace Coworking.Shared.Models.DTOs.Rest;

public record SyncRestroomTopologyRequest
{
    [Required]
    public Guid RestroomId { get; init; }
    [Range(5, 100)]
    public short GridWidth { get; init; }
    [Range(5, 100)]
    public short GridHeight { get; init; }

    public List<TopologyAssetDto> Assets { get; init; } = new();
}

public record TopologyAssetDto
{
    public Guid? Id { get; init; }
    public AssetType Type { get; init; }
    public short PositionX { get; init; }
    public short PositionY { get; init; }
}