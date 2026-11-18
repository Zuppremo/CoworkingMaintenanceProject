using Coworking.Shared.Enums;

namespace Coworking.Shared.Models.DTOs.Analytics;

public class ResolvedIssuesDashboardDto
{
    public int TotalResolved { get; set; }
    public int TotalActive { get; set; }
    public double ResolutionRatePercent { get; set; }
    public double AvgMttrMinutes { get; set; }
    public double AvgMttaMinutes { get; set; }
    public double AvgActiveRepairMinutes { get; set; }
    public double FastestResolutionMinutes { get; set; }
    public double SlowestResolutionMinutes { get; set; }

    public List<AssetTypeMetricDto> ByAssetType { get; set; } = new();
    public List<LocationMetricDto> ByLocation { get; set; } = new();
    public List<IssueCategoryMetricDto> TopIssueDescriptions { get; set; } = new();
    public List<DailyResolutionTrendDto> DailyTrend { get; set; } = new();
    public List<OperatorMetricDto> ByOperator { get; set; } = new();
    public List<ResolvedIssueItemDto> ResolvedIssues { get; set; } = new();
}

public class AssetTypeMetricDto
{
    public AssetType AssetType { get; set; }
    public int ResolvedCount { get; set; }
    public double Percentage { get; set; }
    public double AvgMttrMinutes { get; set; }
}

public class LocationMetricDto
{
    public string BuildingName { get; set; } = string.Empty;
    public int FloorNumber { get; set; }
    public string RestroomIdentifier { get; set; } = string.Empty;
    public int ResolvedCount { get; set; }
    public double AvgMttrMinutes { get; set; }
}

public class IssueCategoryMetricDto
{
    public string Description { get; set; } = string.Empty;
    public int Count { get; set; }
    public double AvgMttrMinutes { get; set; }
}

public class DailyResolutionTrendDto
{
    public DateOnly Date { get; set; }
    public int ResolvedCount { get; set; }
    public double AvgMttrMinutes { get; set; }
}

public class OperatorMetricDto
{
    public string OperatorId { get; set; } = string.Empty;
    public int ResolvedCount { get; set; }
    public double AvgMttrMinutes { get; set; }
}

public class ResolvedIssueItemDto
{
    public Guid Id { get; set; }
    public string IssueDescription { get; set; } = string.Empty;
    public string BuildingName { get; set; } = string.Empty;
    public int FloorNumber { get; set; }
    public string RestroomIdentifier { get; set; } = string.Empty;
    public AssetType AssetType { get; set; }
    public short PositionX { get; set; }
    public short PositionY { get; set; }
    public DateTimeOffset GenerationDate { get; set; }
    public DateTimeOffset? AttentionDate { get; set; }
    public DateTimeOffset ResolutionDate { get; set; }
    public double MttrMinutes { get; set; }
    public double? MttaMinutes { get; set; }
    public string AssignedOperatorId { get; set; } = string.Empty;
    public string ReportingUserId { get; set; } = string.Empty;
}
