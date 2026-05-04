using Coworking.Shared.Models.Interfaces;

namespace Coworking.Shared.Models;

public class Building : ISoftDeletable
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public List<Floor> Floors { get; set; }
    public bool IsDeleted { get; set; }
}