using Microsoft.AspNetCore.Identity;

namespace CoworkingAPI.Data;

public class ApplicationUser : IdentityUser
{
    public ICollection<UserBuilding> UserBuildings { get; set; } = new List<UserBuilding>();
}
