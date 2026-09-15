using System.Security.Claims;
using Coworking.Shared.Enums;
using Coworking.Shared.Models.DTOs;
using CoworkingAPI.Data;
using Microsoft.EntityFrameworkCore;

namespace CoworkingAPI.Features.IssueReports;

public class GetActiveIssuesQueryHandler
{
    private readonly AppDbContext context;
    private readonly IHttpContextAccessor httpContextAccessor;

    public GetActiveIssuesQueryHandler(AppDbContext context, IHttpContextAccessor httpContextAccessor)
    {
        this.context = context;
        this.httpContextAccessor = httpContextAccessor;
    }

    public async Task<List<IssueReportDto>> ExecuteAsync()
    {
        var user = httpContextAccessor.HttpContext?.User;
        var userId = user?.FindFirstValue(ClaimTypes.NameIdentifier);
        
        List<Guid> assignedBuildingIds = new List<Guid>();
        bool isSuperAdmin = user?.IsInRole("SuperAdmin") ?? false;

        if (!isSuperAdmin && userId != null)
        {
            assignedBuildingIds = await context.UserBuildings
                .Where(ub => ub.UserId == userId)
                .Select(ub => ub.BuildingId)
                .ToListAsync();
        }

        var query = from report in context.IssueReports
                    where report.Status < ReportStatus.Resolved
                    join asset in context.RestroomAssets on report.RestroomAssetId equals asset.Id
                    join restroom in context.Restrooms on asset.RestroomId equals restroom.Id
                    join floor in context.Floors on restroom.FloorId equals floor.Id
                    join building in context.Buildings on floor.BuildingId equals building.Id
                    where isSuperAdmin || assignedBuildingIds.Contains(building.Id)
                    orderby report.GenerationDate descending
                    select new IssueReportDto
                    {
                        Id = report.Id,
                        IssueDescription = report.IssueDescription,
                        Status = report.Status,
                        GenerationDate = report.GenerationDate,
                        AttentionDate = report.AttentionDate,
                        ResolutionDate = report.ResolutionDate,
                        RestroomAssetId = asset.Id,
                        AssetType = asset.Type,
                        PositionX = asset.PositionX,
                        PositionY = asset.PositionY,
                        RestroomId = restroom.Id,
                        RestroomIdentifier = restroom.Identifier,
                        FloorNumber = floor.FloorNumber,
                        BuildingName = building.Name
                    };

        return await query.AsNoTracking().ToListAsync();
    }
}
