namespace Coworking.Shared.Models;

public class Restroom
{
    public Guid Id { get; set; }
    public string Identifier { get; set; } = string.Empty; // e.g. "North Men's Restroom"
    public Guid FloorId { get; set; }
    
    public int GridWidth { get; set; }
    public int GridHeight { get; set; }

    public List<RestroomAsset> RestroomAssets { get; set; } = new();
}