using Coworking.Shared.Enums;

namespace Coworking.Shared.Models;

public class IssueReport
{
    public Guid Id { get; set; }
    public string IssueDescription { get; set; } = string.Empty;
    public ReportStatus Status { get; set; } = ReportStatus.Generated;

    public DateTime GenerationDate { get; set; } = DateTime.UtcNow;
    public DateTime? AttentionDate { get; set; }
    public DateTime? ResolutionDate { get; set; }
    
    public Guid RestroomAssetId { get; set; }
    public string ReportingUserId { get; set; } = string.Empty; // Coworker
    public string AssignedOperatorId { get; set; } = string.Empty; // Cleaning worker
}