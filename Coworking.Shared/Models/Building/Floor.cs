namespace Coworking.Shared.Models;

public class Floor
{
    public Guid Id { get; set; }
    public int FloorNumber { get; set; }
    public Guid BuildingId { get; set; }
    public List<Restroom> Restrooms { get; set; }
}