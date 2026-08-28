using Coworking.Shared.Models.DTOs.Building;
using CoworkingAPI.Data;
using Microsoft.EntityFrameworkCore;

namespace CoworkingAPI.Features.AdministrativeHierarchy;

public class GetHierarchyQueryHandler
{
    private readonly AppDbContext context;

    public GetHierarchyQueryHandler(AppDbContext context)
    {
        this.context = context;
    }

    public async Task<List<BuildingDto>> ExecuteAsync()
    {
        var buildings = await context.Buildings
            .Include(b => b.Floors)
            .ThenInclude(f => f.Restrooms)
            .AsNoTracking()
            .ToListAsync();

        return buildings.Select(b => new BuildingDto
        {
            Id = b.Id,
            Name = b.Name,
            Floors = b.Floors.Select(f => new FloorDto
            {
                Id = f.Id,
                FloorNumber = f.FloorNumber,
                Restrooms = f.Restrooms.Select(r => new RestroomHierarchyDto
                {
                    Id = r.Id,
                    Identifier = r.Identifier
                }).ToList()
            }).ToList()
        }).ToList();
    }
}
