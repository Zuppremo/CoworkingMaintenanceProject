using System.ComponentModel.DataAnnotations;
using Coworking.Shared.Enums;

namespace Coworking.Shared.Models.DTOs;

public class UpdateReportStatusDto
{
    [Required]
    public Guid ReportId {get;set;}
    [Required]
    public ReportStatus Status { get; set; }
    [Required]
    public string OperatorId { get; set; } = string.Empty;
}