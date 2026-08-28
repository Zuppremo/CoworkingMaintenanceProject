using Coworking.Shared.Enums;
using Coworking.Shared.Extensions;
using Coworking.Shared.Models;
using Coworking.Shared.Models.DTOs.Rest;
using CoworkingAPI.Data;
using Microsoft.EntityFrameworkCore;

namespace CoworkingAPI.Features.BuildingTopology.SyncTopology;

public class SyncRestroomTopologyCommandHandler
{
    private readonly AppDbContext context;
    private readonly ITopologyCache topologyCache;

    public SyncRestroomTopologyCommandHandler(AppDbContext context, ITopologyCache topologyCache)
    {
        this.context = context;
        this.topologyCache = topologyCache;
    }

    public async Task<string?> ExecuteAsync(SyncRestroomTopologyRequest request)
    {
        var hasCollisions = request.Assets
            .GroupBy(a => new 
                {   
                    a.PositionX, 
                    a.PositionY, 
                    Layer = a.Type.GetMountingLayer()
                })
            .Any(g => g.Count() > 1);

        if (hasCollisions)
            return
                "Collision detected, multiple assets of the same mounting layer (Floor/Wall) cannot occupy the same grid coordinates.";
            
        var outOfBounds = request.Assets.Any(a => 
        a.PositionX < 0 || a.PositionX >= request.GridWidth ||
        a.PositionY < 0 || a.PositionY >= request.GridHeight);

        if (outOfBounds) return "Assets cannot be placed outside the defined grid boundaries.";

        var restroom = await context.Restrooms
            .Include(r => r.Assets)
            .FirstOrDefaultAsync(r => r.Id == request.RestroomId);

        if (restroom == null) return "Restroom not found.";

        restroom.GridWidth = request.GridWidth;
        restroom.GridHeight = request.GridHeight;

        var incomingIds = request.Assets.Where(a => a.Id.HasValue).Select(a => a.Id!.Value).ToHashSet();
        
        List<Guid> pendingCacheRemovals = new();
        List<Guid> pendingCacheAdditions = new();
        
        var assetsToRemove = restroom.Assets.Where(existing => !incomingIds.Contains(existing.Id)).ToList();
        foreach (var asset in assetsToRemove)
        {
            asset.IsDeleted = true;
            context.Entry(asset).State = EntityState.Modified;
            pendingCacheRemovals.Add(asset.Id);
        }

        foreach (var incoming in request.Assets)
        {
            var existing = restroom.Assets.FirstOrDefault(a => a.Id == incoming.Id);
            
            if (existing == null)
            {
                RestroomAsset newAsset = incoming.Type switch
                {
                    AssetType.Toilet => new Toilet(),
                    AssetType.Sink => new Sink(),
                    AssetType.HandDryer => new HandDryer(),
                    AssetType.PaperDispenser => new PaperDispenser(),
                    _ => throw new InvalidOperationException("Unknown asset type")
                };

                if (incoming.Id.HasValue) newAsset.Id = incoming.Id.Value;
                
                newAsset.PositionX = incoming.PositionX;
                newAsset.PositionY = incoming.PositionY;
                newAsset.RestroomId = request.RestroomId;
                
                context.RestroomAssets.Add(newAsset);
                pendingCacheAdditions.Add(newAsset.Id);
            }
            else
            {
                existing.PositionX = incoming.PositionX;
                existing.PositionY = incoming.PositionY;
            }
        }

        await context.SaveChangesAsync();
        foreach (var id in pendingCacheAdditions) topologyCache.AddAsset(id);
        foreach (var id in pendingCacheRemovals) topologyCache.RemoveAsset(id);
        
        return null; // Success
    }
    
}