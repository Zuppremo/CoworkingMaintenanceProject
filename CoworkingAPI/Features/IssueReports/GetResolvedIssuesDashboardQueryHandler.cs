using System.Security.Claims;
using Coworking.Shared.Enums;
using Coworking.Shared.Models.DTOs.Analytics;
using CoworkingAPI.Data;
using Microsoft.EntityFrameworkCore;

namespace CoworkingAPI.Features.IssueReports;

/// <summary>
/// Query handler that aggregates resolved restroom issue metrics (MTTR, MTTA, hotspots, trends)
/// for administrative decision-making, scoped to the user's assigned buildings.
/// </summary>
public class GetResolvedIssuesDashboardQueryHandler
{
    private readonly AppDbContext context;
    private readonly IHttpContextAccessor httpContextAccessor;

    public GetResolvedIssuesDashboardQueryHandler(AppDbContext context, IHttpContextAccessor httpContextAccessor)
    {
        this.context = context;
        this.httpContextAccessor = httpContextAccessor;
    }

    public async Task<ResolvedIssuesDashboardDto> ExecuteAsync(
        Guid? buildingId = null,
        DateTimeOffset? fromDate = null,
        DateTimeOffset? toDate = null,
        AssetType? assetType = null)
    {
        var user = httpContextAccessor.HttpContext?.User;
        var userId = user?.FindFirstValue(ClaimTypes.NameIdentifier);

        List<Guid> assignedBuildingIds = new();
        bool isSuperAdmin = user?.IsInRole("SuperAdmin") ?? false;

        if (!isSuperAdmin && userId != null)
        {
            assignedBuildingIds = await context.UserBuildings
                .Where(ub => ub.UserId == userId)
                .Select(ub => ub.BuildingId)
                .ToListAsync();
        }

        var baseQuery = from report in context.IssueReports
                        join asset in context.RestroomAssets on report.RestroomAssetId equals asset.Id
                        join restroom in context.Restrooms on asset.RestroomId equals restroom.Id
                        join floor in context.Floors on restroom.FloorId equals floor.Id
                        join building in context.Buildings on floor.BuildingId equals building.Id
                        where isSuperAdmin || assignedBuildingIds.Contains(building.Id)
                        select new
                        {
                            report.Id,
                            report.IssueDescription,
                            report.Status,
                            report.GenerationDate,
                            report.AttentionDate,
                            report.ResolutionDate,
                            report.AssignedOperatorId,
                            report.ReportingUserId,
                            AssetType = asset.Type,
                            asset.PositionX,
                            asset.PositionY,
                            BuildingId = building.Id,
                            BuildingName = building.Name,
                            floor.FloorNumber,
                            RestroomIdentifier = restroom.Identifier
                        };

        if (buildingId.HasValue)
        {
            baseQuery = baseQuery.Where(x => x.BuildingId == buildingId.Value);
        }

        if (assetType.HasValue)
        {
            baseQuery = baseQuery.Where(x => x.AssetType == assetType.Value);
        }

        var activeQuery = baseQuery.Where(x => x.Status < ReportStatus.Resolved);
        var resolvedQuery = baseQuery.Where(x => x.Status == ReportStatus.Resolved && x.ResolutionDate.HasValue);

        if (fromDate.HasValue)
        {
            resolvedQuery = resolvedQuery.Where(x => x.ResolutionDate >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            resolvedQuery = resolvedQuery.Where(x => x.ResolutionDate <= toDate.Value);
        }

        var totalActive = await activeQuery.CountAsync();
        var rawResolved = await resolvedQuery
            .OrderByDescending(x => x.ResolutionDate)
            .AsNoTracking()
            .ToListAsync();

        var resolvedItems = rawResolved.Select(x =>
        {
            var resDate = x.ResolutionDate!.Value;
            var mttr = Math.Max(0, (resDate - x.GenerationDate).TotalMinutes);
            double? mtta = x.AttentionDate.HasValue
                ? Math.Max(0, (x.AttentionDate.Value - x.GenerationDate).TotalMinutes)
                : null;

            return new ResolvedIssueItemDto
            {
                Id = x.Id,
                IssueDescription = x.IssueDescription,
                BuildingName = x.BuildingName,
                FloorNumber = x.FloorNumber,
                RestroomIdentifier = x.RestroomIdentifier,
                AssetType = x.AssetType,
                PositionX = x.PositionX,
                PositionY = x.PositionY,
                GenerationDate = x.GenerationDate,
                AttentionDate = x.AttentionDate,
                ResolutionDate = resDate,
                MttrMinutes = Math.Round(mttr, 1),
                MttaMinutes = mtta.HasValue ? Math.Round(mtta.Value, 1) : null,
                AssignedOperatorId = string.IsNullOrWhiteSpace(x.AssignedOperatorId) ? "Sin asignar" : x.AssignedOperatorId,
                ReportingUserId = x.ReportingUserId
            };
        }).ToList();

        var totalResolved = resolvedItems.Count;
        var totalAll = totalResolved + totalActive;

        var itemsWithAttention = resolvedItems.Where(i => i.MttaMinutes.HasValue).ToList();
        var activeRepairDurations = resolvedItems
            .Where(i => i.AttentionDate.HasValue)
            .Select(i => Math.Max(0, (i.ResolutionDate - i.AttentionDate!.Value).TotalMinutes))
            .ToList();

        var dashboard = new ResolvedIssuesDashboardDto
        {
            TotalResolved = totalResolved,
            TotalActive = totalActive,
            ResolutionRatePercent = totalAll > 0 ? Math.Round((double)totalResolved / totalAll * 100, 1) : 0,
            AvgMttrMinutes = totalResolved > 0 ? Math.Round(resolvedItems.Average(i => i.MttrMinutes), 1) : 0,
            AvgMttaMinutes = itemsWithAttention.Count > 0 ? Math.Round(itemsWithAttention.Average(i => i.MttaMinutes!.Value), 1) : 0,
            AvgActiveRepairMinutes = activeRepairDurations.Count > 0 ? Math.Round(activeRepairDurations.Average(), 1) : 0,
            FastestResolutionMinutes = totalResolved > 0 ? Math.Round(resolvedItems.Min(i => i.MttrMinutes), 1) : 0,
            SlowestResolutionMinutes = totalResolved > 0 ? Math.Round(resolvedItems.Max(i => i.MttrMinutes), 1) : 0,
            ResolvedIssues = resolvedItems
        };

        dashboard.ByAssetType = resolvedItems
            .GroupBy(i => i.AssetType)
            .Select(g => new AssetTypeMetricDto
            {
                AssetType = g.Key,
                ResolvedCount = g.Count(),
                Percentage = totalResolved > 0 ? Math.Round((double)g.Count() / totalResolved * 100, 1) : 0,
                AvgMttrMinutes = Math.Round(g.Average(i => i.MttrMinutes), 1)
            })
            .OrderByDescending(m => m.ResolvedCount)
            .ToList();

        dashboard.ByLocation = resolvedItems
            .GroupBy(i => new { i.BuildingName, i.FloorNumber, i.RestroomIdentifier })
            .Select(g => new LocationMetricDto
            {
                BuildingName = g.Key.BuildingName,
                FloorNumber = g.Key.FloorNumber,
                RestroomIdentifier = g.Key.RestroomIdentifier,
                ResolvedCount = g.Count(),
                AvgMttrMinutes = Math.Round(g.Average(i => i.MttrMinutes), 1)
            })
            .OrderByDescending(m => m.ResolvedCount)
            .ThenByDescending(m => m.AvgMttrMinutes)
            .ToList();

        dashboard.TopIssueDescriptions = resolvedItems
            .GroupBy(i => i.IssueDescription.Trim())
            .Select(g => new IssueCategoryMetricDto
            {
                Description = g.Key,
                Count = g.Count(),
                AvgMttrMinutes = Math.Round(g.Average(i => i.MttrMinutes), 1)
            })
            .OrderByDescending(m => m.Count)
            .Take(8)
            .ToList();

        dashboard.DailyTrend = resolvedItems
            .GroupBy(i => DateOnly.FromDateTime(i.ResolutionDate.UtcDateTime))
            .Select(g => new DailyResolutionTrendDto
            {
                Date = g.Key,
                ResolvedCount = g.Count(),
                AvgMttrMinutes = Math.Round(g.Average(i => i.MttrMinutes), 1)
            })
            .OrderBy(t => t.Date)
            .ToList();

        dashboard.ByOperator = resolvedItems
            .GroupBy(i => i.AssignedOperatorId)
            .Select(g => new OperatorMetricDto
            {
                OperatorId = g.Key,
                ResolvedCount = g.Count(),
                AvgMttrMinutes = Math.Round(g.Average(i => i.MttrMinutes), 1)
            })
            .OrderByDescending(o => o.ResolvedCount)
            .ToList();

        return dashboard;
    }
}
