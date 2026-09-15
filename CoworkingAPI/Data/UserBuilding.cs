using Coworking.Shared.Models;

namespace CoworkingAPI.Data;

public class UserBuilding
{
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;

    public Guid BuildingId { get; set; }
    public Building Building { get; set; } = null!;
}
