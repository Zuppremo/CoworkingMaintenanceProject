using Coworking.Shared.Models.Interfaces;

namespace Coworking.Shared.Models;

public class Floor : ISoftDeletable
{
    public Guid Id { get; set; }
    public int FloorNumber { get; set; }
    public Guid BuildingId { get; set; }
    public List<Restroom> Restrooms { get; set; }
    public bool IsDeleted { get; set; }
}