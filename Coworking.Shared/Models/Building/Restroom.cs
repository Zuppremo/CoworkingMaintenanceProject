using Coworking.Shared.Models.Interfaces;

namespace Coworking.Shared.Models;

public class Restroom : ISoftDeletable
{
    public Guid Id { get; set; }
    public string Identifier { get; set; } = string.Empty; // e.g. "North Men's Restroom"
    public Guid FloorId { get; set; }
    
    public short GridWidth { get; set; }
    public short GridHeight { get; set; }

    public List<RestroomAsset> Assets { get; set; } = new();
    public bool IsDeleted { get; set; }
}