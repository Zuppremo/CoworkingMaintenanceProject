using Coworking.Shared.Models.DTOs.Telemetry;
using Coworking.Shared.Models.Interfaces;
using CoworkingAPI.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace CoworkingAPI.Infrastructure;

public class SignalRAlertPublisher : IAlertPublisher
{
    private readonly IHubContext<AlertTelemetryHub> hubContext;

    private SignalRAlertPublisher(IHubContext<AlertTelemetryHub> hubContext)
    {
        this.hubContext = hubContext;
    }
    
    public async Task BroadcastStatusUpdateAsync(ReportStatusUpdateTelemetry payload)
    {
        await hubContext.Clients.All.SendAsync("OnReportStatusUpdated", payload);
    }
}