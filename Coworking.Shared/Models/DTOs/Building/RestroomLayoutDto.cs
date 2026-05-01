namespace Coworking.Shared.Models.DTOs.Building;

public class RestroomLayoutDto
{
    public Guid RestroomId { get; set; }
    public string Identifier { get; set; } = string.Empty; // e.g., "Main Lobby Restroom"
    
    public int GridWidth { get; set; }
    public int GridHeight { get; set; } //Boundaries of the grid

    public List<RestroomAssetDto> Assets { get; set; } = new();
}