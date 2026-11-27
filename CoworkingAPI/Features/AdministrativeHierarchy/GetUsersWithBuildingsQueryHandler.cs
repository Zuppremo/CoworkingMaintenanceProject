using Coworking.Shared.Models.DTOs.Building;
using CoworkingAPI.Data;
using Microsoft.EntityFrameworkCore;

namespace CoworkingAPI.Features.AdministrativeHierarchy;

public class GetUsersWithBuildingsQueryHandler(AppDbContext context)
{
    public async Task<List<UserBuildingSummaryDto>> ExecuteAsync()
    {
        var users = await context.Users
            .AsNoTracking()
            .Select(u => new { u.Id, Email = u.Email ?? u.UserName ?? string.Empty })
            .OrderBy(u => u.Email)
            .ToListAsync();

        var userRoles = await (
            from ur in context.UserRoles.AsNoTracking()
            join r in context.Roles.AsNoTracking() on ur.RoleId equals r.Id
            where r.Name != null
            select new { ur.UserId, RoleName = r.Name! }
        ).ToListAsync();

        var userBuildings = await context.UserBuildings
            .AsNoTracking()
            .ToListAsync();

        var rolesByUser = userRoles
            .GroupBy(x => x.UserId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.RoleName).ToList());

        var buildingsByUser = userBuildings
            .GroupBy(x => x.UserId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.BuildingId).ToList());

        return users.Select(u => new UserBuildingSummaryDto
        {
            UserId = u.Id,
            Email = u.Email,
            Roles = rolesByUser.TryGetValue(u.Id, out var roles) ? roles : new List<string>(),
            AssignedBuildingIds = buildingsByUser.TryGetValue(u.Id, out var bIds) ? bIds : new List<Guid>()
        }).ToList();
    }
}
