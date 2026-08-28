using Coworking.Shared.Models;
using Coworking.Shared.Models.DTOs.Rest;
using CoworkingAPI.Data;
using Microsoft.EntityFrameworkCore;

namespace CoworkingAPI.Features.AdministrativeHierarchy;

public class CreateFloorCommandHandler
{
    private readonly AppDbContext context;

    public CreateFloorCommandHandler(AppDbContext context)
    {
        this.context = context;
    }

    public async Task<Guid?> ExecuteAsync(CreateFloorRequest request)
    {
        var buildingExists = await context.Buildings.AnyAsync(b => b.Id == request.BuildingId);
        if (!buildingExists) return null;

        var floor = new Floor
        {
            Id = Guid.NewGuid(),
            FloorNumber = request.FloorNumber,
            BuildingId = request.BuildingId
        };

        context.Floors.Add(floor);
        await context.SaveChangesAsync();

        return floor.Id;
    }
}
