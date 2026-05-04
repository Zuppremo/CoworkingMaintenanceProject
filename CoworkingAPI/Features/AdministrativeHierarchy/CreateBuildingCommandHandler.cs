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

public class CreateFloorCommandHandler(AppDbContext context)
{
    public async Task<Guid> ExecuteAsync(CreateFloorRequest request)
    {
        Floor floor = new Floor { FloorNumber = request.FloorNumber, BuildingId = request.BuildingId };
        context.Floors.Add(floor);
        await context.SaveChangesAsync();
        return floor.Id;
    }
}

public class CreateRestroomCommandHandler(AppDbContext context)
{
    public async Task<Guid> ExecuteAsync(CreateRestroomRequest request)
    {
        Restroom restroom = new Restroom { Identifier = request.Identifier, FloorId = request.FloorId };
        context.Restrooms.Add(restroom);
        await context.SaveChangesAsync();
        return restroom.Id;
    }
}