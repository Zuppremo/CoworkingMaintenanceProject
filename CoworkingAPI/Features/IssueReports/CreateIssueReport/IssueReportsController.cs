using Coworking.Shared.Models.DTOs;
using CoworkingAPI.Features.BuildingTopology;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace CoworkingAPI.Features.IssueReports;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class IssueReportsController : ControllerBase
{
    private readonly CreateIssueReportCommandHandler createHandler;
    private readonly GetActiveIssuesQueryHandler getActiveHandler;
    private readonly AttendIssueReportCommandHandler attendHandler;
    private readonly ResolveIssueReportCommandHandler resolveHandler;
    private readonly ITopologyCache topologyCache;

    public IssueReportsController(
        CreateIssueReportCommandHandler createHandler,
        GetActiveIssuesQueryHandler getActiveHandler,
        AttendIssueReportCommandHandler attendHandler,
        ResolveIssueReportCommandHandler resolveHandler,
        ITopologyCache topologyCache)
    {
        this.createHandler = createHandler;
        this.getActiveHandler = getActiveHandler;
        this.attendHandler = attendHandler;
        this.resolveHandler = resolveHandler;
        this.topologyCache = topologyCache;
    }

    [AllowAnonymous]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateIssueReportRequest request)
    {
        if (!topologyCache.IsValidAsset(request.RestroomAssetId))
            return BadRequest(new { Error = "The specified restroom asset does not exist in the active topology" });

        if (string.IsNullOrWhiteSpace(request.IssueDescription))
            return BadRequest(new { Error = "Description is required" });

        var result = await createHandler.ExecuteAsync(request);
        if (!result.Success)
            return Conflict(new { Error = result.Error });

        return Ok(new { Message = "Report created successfully" });
    }

    [HttpGet("active")]
    public async Task<IActionResult> GetActiveIssues()
    {
        var issues = await getActiveHandler.ExecuteAsync();
        return Ok(issues);
    }

    [HttpPost("{id}/attend")]
    public async Task<IActionResult> Attend(Guid id, [FromQuery] string operatorId = "")
    {
        var success = await attendHandler.ExecuteAsync(id, operatorId);
        if (!success)
            return NotFound(new { Error = "Issue report not found" });

        return Ok(new { Message = "Issue status marked as InProgress" });
    }

    [HttpPost("{id}/resolve")]
    public async Task<IActionResult> Resolve(Guid id, [FromQuery] string operatorId = "")
    {
        var success = await resolveHandler.ExecuteAsync(id, operatorId);
        if (!success)
            return NotFound(new { Error = "Issue report not found" });

        return Ok(new { Message = "Issue resolved successfully" });
    }
}