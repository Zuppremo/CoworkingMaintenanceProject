namespace Coworking.Shared.Models.DTOs.Building;

public record BuildingDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public List<FloorDto> Floors { get; init; } = new();
}

public record FloorDto
{
    public Guid Id { get; init; }
    public int FloorNumber { get; init; }
    public List<RestroomHierarchyDto> Restrooms { get; init; } = new();
}

// Named differently to avoid clash with potential existing RestroomDto
public record RestroomHierarchyDto
{
    public Guid Id { get; init; }
    public string Identifier { get; init; } = string.Empty;
}
