using Coworking.Shared.Enums;
using MessagePack;

namespace Coworking.Shared.Models.DTOs.Telemetry;

[MessagePackObject]
public record ReportStatusUpdateTelemetry
{
    [Key(0)]
    public Guid ReportId {get;init;}
    [Key(1)]
    public ReportStatus Status { get; init; }
}