using System.ComponentModel.DataAnnotations;

namespace Coworking.Shared.Models.DTOs;

//What the coworker sends to the api to create a new alert
public record CreateIssueReportRequest
{
    [Required]
    public Guid RestroomAssetId { get; set; }

    [Required] 
    public string IssueDescription { get; set; } = string.Empty;

    [Required] 
    public string ReportingUserId { get; set; } = string.Empty;
}