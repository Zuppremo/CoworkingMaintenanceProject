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

        var telemetryPayload = new ReportStatusUpdateTelemetry
        {
            ReportId = report.Id,
            Status = report.Status
        };

        await alertPublisher.BroadcastStatusUpdateAsync(telemetryPayload);
        return true;
    }
}
