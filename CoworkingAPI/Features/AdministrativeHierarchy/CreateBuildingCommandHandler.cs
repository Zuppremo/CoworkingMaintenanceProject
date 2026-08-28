using Coworking.Shared.Models;
using Coworking.Shared.Models.DTOs.Rest;
using CoworkingAPI.Data;

namespace CoworkingAPI.Features.AdministrativeHierarchy;

public class CreateBuildingCommandHandler(AppDbContext context)
{
    public async Task<Guid> ExecuteAsync(CreateBuildingRequest request)
    {
        Building building = new Building { Name = request.Name };
        context.Buildings.Add(building);
        await context.SaveChangesAsync();
        return building.Id;
    }
}


