using Coworking.Shared.Models.DTOs.Telemetry;

namespace Coworking.Shared.Models.DTOs.Building;

public record RestroomLayoutDto
{
    public Guid RestroomId { get; set; }
    public string Identifier { get; set; } = string.Empty; // e.g., "Main Lobby Restroom"
    public short GridWidth { get; set; }
    public short GridHeight { get; set; } //Boundaries of the grid
    public List<RestroomAssetDto> Assets { get; set; } = new();
}