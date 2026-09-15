using Coworking.Shared.Models.DTOs.Telemetry;

namespace Coworking.Shared.Models.Interfaces;

public interface IAlertPublisher
{
    Task BroadcastStatusUpdateAsync(ReportStatusUpdateTelemetry payload, Guid buildingId);
}