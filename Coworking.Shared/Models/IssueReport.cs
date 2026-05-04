using Coworking.Shared.Enums;
using Coworking.Shared.Models.Interfaces;

namespace Coworking.Shared.Models;

public class IssueReport : ISoftDeletable
{
    public Guid Id { get; set; }
    public string IssueDescription { get; set; } = string.Empty;
    public ReportStatus Status { get; set; } = ReportStatus.Generated;

    public DateTimeOffset GenerationDate { get; set; } = DateTime.UtcNow;
    public DateTimeOffset? AttentionDate { get; set; }
    public DateTimeOffset? ResolutionDate { get; set; }
    
    public Guid RestroomAssetId { get; set; }
    public string ReportingUserId { get; set; } = string.Empty; // Coworker
    public string AssignedOperatorId { get; set; } = string.Empty; // Cleaning worker
    public bool IsDeleted { get; set; }
}