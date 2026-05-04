using System.ComponentModel.DataAnnotations;

namespace Coworking.Shared.Models.DTOs.Rest;

public record CreateBuildingRequest
{
    [Required, MaxLength(100)] public string Name { get; init; } = string.Empty;
}

public record CreateFloorRequest
{
    [Required]
    public int FloorNumber { get; init; }
    [Required]
    public Guid BuildingId { get; init; }
}

public record CreateRestroomRequest
{
    [Required, MaxLength(100)]
    public string Identifier { get; init;  } = string.Empty;
    [Required]
    public Guid FloorId { get; init;  }
}