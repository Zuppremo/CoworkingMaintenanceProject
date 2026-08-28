using Coworking.Shared.Models.DTOs.Rest;
using Microsoft.AspNetCore.Mvc;

namespace CoworkingAPI.Features.BuildingTopology.SyncTopology;

[ApiController]
[Route("api/[controller]")]
public class TopologyController : ControllerBase
{
    private readonly SyncRestroomTopologyCommandHandler handler;
    private readonly GetRestroomTopologyQueryHandler getHandler;

    public TopologyController(SyncRestroomTopologyCommandHandler handler, GetRestroomTopologyQueryHandler getHandler)
    {
        this.handler = handler;
        this.getHandler = getHandler;
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetTopology(Guid id)
    {
        var layout = await getHandler.ExecuteAsync(id);
        if (layout == null) return NotFound();
        return Ok(layout);
    }

    [HttpPost("sync")]
    public async Task<IActionResult> SyncTopology([FromBody] SyncRestroomTopologyRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var error = await handler.ExecuteAsync(request);

        if (error != null)
        {
            return BadRequest(new { Message = error });
        }

        return Ok(new { Message = "Topology synced successfully." });
    }
}
