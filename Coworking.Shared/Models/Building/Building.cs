namespace Coworking.Shared.Models;

public class Building
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public List<Floor> Floors { get; set; }
}