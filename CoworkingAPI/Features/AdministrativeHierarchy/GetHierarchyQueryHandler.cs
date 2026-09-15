using System.Security.Claims;
using Coworking.Shared.Models.DTOs.Building;
using CoworkingAPI.Data;
using Microsoft.EntityFrameworkCore;

namespace CoworkingAPI.Features.AdministrativeHierarchy;

public class GetHierarchyQueryHandler
{
    private readonly AppDbContext context;
    private readonly IHttpContextAccessor httpContextAccessor;

    public GetHierarchyQueryHandler(AppDbContext context, IHttpContextAccessor httpContextAccessor)
    {
        this.context = context;
        this.httpContextAccessor = httpContextAccessor;
    }

    public async Task<List<BuildingDto>> ExecuteAsync()
    {
        var user = httpContextAccessor.HttpContext?.User;
        bool isSuperAdmin = user?.IsInRole("SuperAdmin") ?? false;
        var userId = user?.FindFirstValue(ClaimTypes.NameIdentifier);

        var query = context.Buildings
            .Include(b => b.Floors)
            .ThenInclude(f => f.Restrooms)
            .AsNoTracking();

        if (!isSuperAdmin && userId != null)
        {
            var assignedBuildingIds = await context.UserBuildings
                .Where(ub => ub.UserId == userId)
                .Select(ub => ub.BuildingId)
                .ToListAsync();

            query = query.Where(b => assignedBuildingIds.Contains(b.Id));
        }

        var buildings = await query.ToListAsync();

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
