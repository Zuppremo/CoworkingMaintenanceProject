using Coworking.Shared.Enums;

namespace Coworking.Shared.Models.DTOs;

public class IssueReportDto
{
    public Guid Id { get; set; }
    public string IssueDescription { get; set; } = string.Empty;
    public ReportStatus Status { get; set; }
    
    public DateTimeOffset GenerationDate { get; set; }
    public DateTimeOffset? AttentionDate { get; set; }
    public DateTimeOffset? ResolutionDate { get; set; }
    
    public Guid RestroomId { get; set; }
    public AssetType AssetType { get; set; }
}