using Coworking.Shared.Models.DTOs.Building;
using CoworkingAPI.Data;
using Microsoft.EntityFrameworkCore;

namespace CoworkingAPI.Features.AdministrativeHierarchy;

public class UpdateBuildingUsersCommandHandler(AppDbContext context)
{
    public async Task<bool> ExecuteAsync(Guid buildingId, UpdateBuildingUsersRequest request)
    {
        var buildingExists = await context.Buildings.AnyAsync(b => b.Id == buildingId);
        if (!buildingExists) return false;

        var requestedIds = request.UserIds
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct()
            .ToList();

        var validUserIds = await context.Users
            .Where(u => requestedIds.Contains(u.Id))
            .Select(u => u.Id)
            .ToHashSetAsync();

        var existingAssignments = await context.UserBuildings
            .Where(ub => ub.BuildingId == buildingId)
            .ToListAsync();

        var toRemove = existingAssignments
            .Where(ub => !validUserIds.Contains(ub.UserId))
            .ToList();

        if (toRemove.Count > 0)
        {
            context.UserBuildings.RemoveRange(toRemove);
        }

        var existingUserIds = existingAssignments.Select(ub => ub.UserId).ToHashSet();
        var toAdd = validUserIds
            .Where(userId => !existingUserIds.Contains(userId))
            .Select(userId => new UserBuilding
            {
                UserId = userId,
                BuildingId = buildingId
            });

        await context.UserBuildings.AddRangeAsync(toAdd);
        await context.SaveChangesAsync();
        return true;
    }
}
