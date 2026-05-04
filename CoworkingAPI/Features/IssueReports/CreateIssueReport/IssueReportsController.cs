using Coworking.Shared.Models.DTOs;
using CoworkingAPI.Features.BuildingTopology;
using Microsoft.AspNetCore.Mvc;

namespace CoworkingAPI.Features.IssueReports.CreateIssueReport;

[ApiController]
[Route("api/[controller]")]
public class IssueReportsController : ControllerBase
{
    private readonly CreateIssueReportCommandHandler handler;
    private readonly ITopologyCache topologyCache;

    public IssueReportsController(CreateIssueReportCommandHandler handler, ITopologyCache topologyCache)
    {
        this.handler = handler;
        this.topologyCache = topologyCache;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateIssueReportRequest request)
    {
        if (!topologyCache.IsValidAsset(request.RestroomAssetId))
            return BadRequest(new { Error = "The specified restroom asset does not exist in the active topology"});

        if (string.IsNullOrWhiteSpace(request.IssueDescription))
            return BadRequest(new { Error = "Description is required" });
        
        await handler.ExecuteAsync(request);
        return Ok();
    }
}