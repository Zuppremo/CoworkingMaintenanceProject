using Coworking.Shared.Models;
using Coworking.Shared.Models.DTOs;
using Coworking.Shared.Models.DTOs.Telemetry;
using Coworking.Shared.Models.Interfaces;
using CoworkingAPI.Data;

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

    public async Task ExecuteAsync(CreateIssueReportRequest request)
    {
        var report = new IssueReport()
        {
            RestroomAssetId =  request.RestroomAssetId,
            IssueDescription =  request.IssueDescription,
            ReportingUserId = request.ReportingUserId,
        };
        
        context.IssueReports.Add(report);
        await context.SaveChangesAsync();

        var telemetryPayload = new ReportStatusUpdateTelemetry()
        {
            ReportId = report.Id,
            Status = report.Status,
        };
        
        await alertPublisher.BroadcastStatusUpdateAsync(telemetryPayload);
    }
}