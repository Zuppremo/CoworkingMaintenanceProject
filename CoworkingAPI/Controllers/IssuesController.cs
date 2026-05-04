using Coworking.Shared.Enums;
using Coworking.Shared.Models;
using Coworking.Shared.Models.DTOs;
using Coworking.Shared.Models.DTOs.Telemetry;
using CoworkingAPI.Data;
using CoworkingAPI.Hubs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace CoworkingAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class IssuesController : ControllerBase
    {
        private readonly AppDbContext context;
        private readonly IHubContext<AlertTelemetryHub> hubContext;
    
        public IssuesController(AppDbContext context,  IHubContext<AlertTelemetryHub> hubContext)
        {
            this.context = context;
            this.hubContext = hubContext;
        }

        [HttpPost]
        public async Task<IActionResult> CreateIssue([FromBody] CreateIssueReportRequest issueDto)
        {
            var assetExist = await context.RestroomAssets.AnyAsync(a => a.Id == issueDto.RestroomAssetId);

            if (!assetExist)
                return NotFound(new { Message = "The specified restroom asset does not exist." });

            IssueReport newReport = new IssueReport()
            {
                RestroomAssetId = issueDto.RestroomAssetId,
                IssueDescription = issueDto.IssueDescription,
                ReportingUserId = issueDto.ReportingUserId,
                Status = ReportStatus.Generated,
                GenerationDate = DateTime.UtcNow
            };
        
            context.IssueReports.Add(newReport);
            await context.SaveChangesAsync();

            var reportDto = new IssueReportDto()
            {
                Id = newReport.Id,
                IssueDescription = newReport.IssueDescription,
                Status = newReport.Status,
                GenerationDate = newReport.GenerationDate,
                RestroomId = newReport.RestroomAssetId
            };

            await hubContext.Clients.All.SendAsync("ReceiveNewAlert", reportDto);
        
            return Ok (new {Message = "Issue created successfully", ReportId = newReport.Id});
        }

        [HttpGet("active")]
        public async Task<ActionResult<IEnumerable<IssueReportDto>>> GetActiveIssues()
        {
            var issues = await context.IssueReports
                .Where(ir => ir.Status != ReportStatus.Resolved)
                .OrderByDescending(ir => ir.GenerationDate)
                .Select(ir => new IssueReportDto()
                {
                    Id = ir.Id,
                    IssueDescription = ir.IssueDescription,
                    Status = ir.Status,
                    GenerationDate = ir.GenerationDate,
                    RestroomId = ir.RestroomAssetId
                })
                .ToListAsync();
            
            return Ok(issues);
        }

        [HttpPut("status")]
        public async Task<IActionResult> UpdateIssueStatus([FromBody] ReportStatusUpdateTelemetry statusDto)
        {
            var issue = await context.IssueReports.FindAsync(statusDto.ReportId);

            if (issue == null)
                return NotFound(new { Message = "Report not found" });

            issue.Status = statusDto.Status;

            if (statusDto.Status == ReportStatus.InProgress && issue.AttentionDate == null)
                issue.AttentionDate = DateTime.UtcNow;
            else if (statusDto.Status == ReportStatus.Resolved)
                issue.ResolutionDate  = DateTime.UtcNow;

            await context.SaveChangesAsync();
            
            await hubContext.Clients.All.SendAsync("IssueStatusUpdated", new
            {
                ReportId = issue.Id,
                NewStatus = statusDto.Status,
                AssetId = issue.RestroomAssetId
            });
            return Ok(new {Message = "Status updated successfully", ReportId = issue.Id});
        }
    }
}
