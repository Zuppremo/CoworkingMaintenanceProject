using Coworking.Shared.Enums;
using Coworking.Shared.Models.DTOs.Telemetry;
using Coworking.Shared.Models.Interfaces;
using CoworkingAPI.Data;
using Microsoft.EntityFrameworkCore;

namespace CoworkingAPI.Features.IssueReports;

public class ResolveIssueReportCommandHandler
{
    private readonly AppDbContext context;
    private readonly IAlertPublisher alertPublisher;

    public ResolveIssueReportCommandHandler(AppDbContext context, IAlertPublisher alertPublisher)
    {
        this.context = context;
        this.alertPublisher = alertPublisher;
    }

    public async Task<bool> ExecuteAsync(Guid reportId, string operatorId = "")
    {
        var report = await context.IssueReports.FirstOrDefaultAsync(r => r.Id == reportId);
        if (report == null) return false;

        report.Status = ReportStatus.Resolved;
        report.ResolutionDate = DateTimeOffset.UtcNow;
        if (!string.IsNullOrWhiteSpace(operatorId))
        {
            report.AssignedOperatorId = operatorId;
        }

        await context.SaveChangesAsync();

        var buildingId = await context.IssueReports
            .Where(r => r.Id == reportId)
            .Join(context.RestroomAssets, r => r.RestroomAssetId, a => a.Id, (r, a) => a.RestroomId)
            .Join(context.Restrooms, rId => rId, res => res.Id, (rId, res) => res.FloorId)
            .Join(context.Floors, fId => fId, f => f.Id, (fId, f) => f.BuildingId)
            .FirstOrDefaultAsync();

        var telemetryPayload = new ReportStatusUpdateTelemetry
        {
            ReportId = report.Id,
            Status = report.Status
        };

        if (buildingId != Guid.Empty)
        {
            await alertPublisher.BroadcastStatusUpdateAsync(telemetryPayload, buildingId);
        }
        return true;
    }
}
