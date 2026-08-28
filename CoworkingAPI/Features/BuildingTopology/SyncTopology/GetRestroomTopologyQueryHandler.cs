using Coworking.Shared.Models.DTOs.Building;
using Coworking.Shared.Models.DTOs.Telemetry;
using CoworkingAPI.Data;
using Microsoft.EntityFrameworkCore;

namespace CoworkingAPI.Features.BuildingTopology.SyncTopology;

public class GetRestroomTopologyQueryHandler
{
    private readonly AppDbContext context;

    public GetRestroomTopologyQueryHandler(AppDbContext context)
    {
        this.context = context;
    }

    public async Task<RestroomLayoutDto?> ExecuteAsync(Guid restroomId)
    {
        var restroom = await context.Restrooms
            .Include(r => r.Assets)
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == restroomId);

        if (restroom == null) return null;

        return new RestroomLayoutDto
        {
            RestroomId = restroom.Id,
            Identifier = restroom.Identifier,
            GridWidth = restroom.GridWidth,
            GridHeight = restroom.GridHeight,
            Assets = restroom.Assets.Select(a => new RestroomAssetDto
            {
                Id = a.Id,
                Type = a.Type,
                PositionX = a.PositionX,
                PositionY = a.PositionY,
                HasActiveIssue = false 
            }).ToList()
        };
    }
}
