using CoworkingAPI.Data;
using CoworkingAPI.Features.BuildingTopology;
using Microsoft.EntityFrameworkCore;

namespace CoworkingAPI.Features.AdministrativeHierarchy;

public class DeleteBuildingCommandHandler
{
    private readonly AppDbContext context;
    private readonly ITopologyCache topologyCache;

    public DeleteBuildingCommandHandler(AppDbContext context, ITopologyCache topologyCache)
    {
        this.context = context;
        this.topologyCache = topologyCache;
    }

    public async Task<bool> ExecuteAsync(Guid buildingId)
    {
        var buildingExist = await context.Buildings.AnyAsync(b => b.Id == buildingId);

        if (!buildingExist) return false;

        using var transaction = await context.Database.BeginTransactionAsync();

        try
        {
            var floorIds = await context.Floors
                .Where(f => f.BuildingId == buildingId)
                .Select(f => f.Id).ToListAsync();

            var restroomIds = new List<Guid>();
            if (floorIds.Any())
            {
                restroomIds = await context.Restrooms.Where(r => floorIds.Contains(r.FloorId))
                    .Select(r => r.Id)
                    .ToListAsync();
            }

            var assetIdsToEvict = new List<Guid>();

            if (restroomIds.Any())
            {
                assetIdsToEvict = await context.RestroomAssets.Where(a => restroomIds.Contains(a.RestroomId))
                    .Select(a => a.Id).ToListAsync();
            }

            if (assetIdsToEvict.Any())
            {
                await context.IssueReports.Where(ir => assetIdsToEvict.Contains(ir.RestroomAssetId))
                    .ExecuteUpdateAsync(s => s.SetProperty(p => p.IsDeleted, true));
                
                await  context.RestroomAssets.Where(a => restroomIds.Contains(a.RestroomId)).
                    ExecuteUpdateAsync(s => s.SetProperty(p => p.IsDeleted, true));
            }
            
            await context.Floors.Where(f => f.BuildingId == buildingId).
                ExecuteUpdateAsync(s => s.SetProperty(p => p.IsDeleted, true));
            
            await context.Buildings.Where(b => b.Id == buildingId).
                ExecuteUpdateAsync(s => s.SetProperty(p => p.IsDeleted, true));


            foreach (var assetId in assetIdsToEvict)
                topologyCache.RemoveAsset(assetId);

            return true;
        }
        catch (Exception e)
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}