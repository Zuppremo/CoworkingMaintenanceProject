using Coworking.Shared.Enums;
using Coworking.Shared.Models;
using Coworking.Shared.Models.DTOs;
using Coworking.Shared.Models.DTOs.Telemetry;
using Coworking.Shared.Models.Interfaces;
using CoworkingAPI.Data;
using Microsoft.EntityFrameworkCore;

namespace CoworkingAPI.Features.IssueReports;

public class CreateIssueReportCommandHandler
{
    private readonly AppDbContext context;
    private readonly IAlertPublisher alertPublisher;
    
    public CreateIssueReportCommandHandler(AppDbContext context, IAlertPublisher alertPublisher)
    {
        this.context = context;
        this.alertPublisher = alertPublisher;
    }

    public async Task<(bool Success, string Error)> ExecuteAsync(CreateIssueReportRequest request)
    {
        var existingActive = await context.IssueReports
            .AnyAsync(r => r.RestroomAssetId == request.RestroomAssetId && r.Status < ReportStatus.Resolved);

        if (existingActive)
        {
            return (false, "Este elemento ya tiene un reporte activo pendiente de atención por parte del personal de aseo.");
        }

        var report = new IssueReport()
        {
            RestroomAssetId = request.RestroomAssetId,
            IssueDescription = request.IssueDescription,
            ReportingUserId = string.IsNullOrWhiteSpace(request.ReportingUserId) ? "anonymous-coworker" : request.ReportingUserId,
        };
        
        context.IssueReports.Add(report);
        await context.SaveChangesAsync();

        var buildingId = await context.RestroomAssets
            .Where(a => a.Id == request.RestroomAssetId)
            .Join(context.Restrooms, a => a.RestroomId, r => r.Id, (a, r) => r.FloorId)
            .Join(context.Floors, fId => fId, f => f.Id, (fId, f) => f.BuildingId)
            .FirstOrDefaultAsync();

        var telemetryPayload = new ReportStatusUpdateTelemetry()
        {
            ReportId = report.Id,
            Status = report.Status,
        };
        
        if (buildingId != Guid.Empty)
        {
            await alertPublisher.BroadcastStatusUpdateAsync(telemetryPayload, buildingId);
        }
        
        return (true, string.Empty);
    }
}