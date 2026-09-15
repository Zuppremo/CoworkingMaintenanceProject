using Coworking.Shared.Models.DTOs.Building;
using Coworking.Shared.Models.DTOs.Rest;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace CoworkingAPI.Features.AdministrativeHierarchy;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class  HierarchyController : ControllerBase
{
    private readonly CreateBuildingCommandHandler createBuildingCommandHandler;
    private readonly DeleteBuildingCommandHandler deleteBuildingCommandHandler;
    private readonly CreateFloorCommandHandler createFloorCommandHandler;
    private readonly CreateRestroomCommandHandler createRestroomCommandHandler;
    private readonly GetHierarchyQueryHandler getHierarchyQueryHandler;

    public HierarchyController(
        CreateBuildingCommandHandler createBuildingCommandHandler,
        DeleteBuildingCommandHandler deleteBuildingCommandHandler,
        CreateFloorCommandHandler createFloorCommandHandler,
        CreateRestroomCommandHandler createRestroomCommandHandler,
        GetHierarchyQueryHandler getHierarchyQueryHandler)
    {
        this.createBuildingCommandHandler = createBuildingCommandHandler;
        this.deleteBuildingCommandHandler = deleteBuildingCommandHandler;
        this.createFloorCommandHandler = createFloorCommandHandler;
        this.createRestroomCommandHandler = createRestroomCommandHandler;
        this.getHierarchyQueryHandler = getHierarchyQueryHandler;
    }

    [HttpGet]
    public async Task<ActionResult<List<BuildingDto>>> GetHierarchy()
    {
        var result = await getHierarchyQueryHandler.ExecuteAsync();
        return Ok(result);
    }

    [Authorize(Roles = "SuperAdmin")]
    [HttpPost("buildings")]
    public async Task<IActionResult> CreateBuilding([FromBody] CreateBuildingRequest request)
    {
        var id = await createBuildingCommandHandler.ExecuteAsync(request);
        return Ok(new { Id = id });
    }

    [Authorize(Roles = "SuperAdmin")]
    [HttpDelete("buildings/{id}")]
    public async Task<IActionResult> DeleteBuilding(Guid id)
    {
        var success = await deleteBuildingCommandHandler.ExecuteAsync(id);
        if (!success) return NotFound();
        return Ok();
    }

    [Authorize(Roles = "SuperAdmin,Admin")]
    [HttpPost("floors")]
    public async Task<IActionResult> CreateFloor([FromBody] CreateFloorRequest request)
    {
        var id = await createFloorCommandHandler.ExecuteAsync(request);
        if (id == null) return NotFound("Building not found");
        return Ok(new { Id = id });
    }

    [Authorize(Roles = "SuperAdmin,Admin")]
    [HttpPost("restrooms")]
    public async Task<IActionResult> CreateRestroom([FromBody] CreateRestroomRequest request)
    {
        var id = await createRestroomCommandHandler.ExecuteAsync(request);
        if (id == null) return NotFound("Floor not found");
        return Ok(new { Id = id });
    }
}
