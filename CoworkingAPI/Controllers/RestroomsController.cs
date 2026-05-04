using Coworking.Shared.Enums;
using Coworking.Shared.Models.DTOs.Building;
using Coworking.Shared.Models.DTOs.Telemetry;
using CoworkingAPI.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CoworkingAPI.Controllers
{
    [Microsoft.AspNetCore.Components.Route("api/[controller]")]
    [ApiController]
    public class RestroomsController : ControllerBase
    {
        private readonly AppDbContext context;
        
        public RestroomsController(AppDbContext context)
        {
            this.context = context;
        }
        [HttpGet("{id}/layout")]
        public async Task<ActionResult<RestroomLayoutDto>> GetRestroomLayout(Guid id)
        {
            var restroom = await context.Restrooms.Include(r => r.Assets)
                .FirstOrDefaultAsync(r => r.Id == id);
            
            if (restroom == null) return NotFound(new {Message = "Restroom not found"});

            var activeIssues = await context.IssueReports.Where(ir => ir.Status != ReportStatus.Resolved)
                .Select(ir => ir.RestroomAssetId)
                .ToListAsync();

            var layoutDto = new RestroomLayoutDto()
            {
                RestroomId = restroom.Id,
                Identifier = restroom.Identifier,
                GridWidth = restroom.GridWidth,
                GridHeight = restroom.GridHeight,
                Assets = restroom.Assets.Select(a => new RestroomAssetDto()
                {
                    Id = a.Id,
                    Type = a.Type,
                    PositionX = a.PositionX,
                    PositionY = a.PositionY,
                    HasActiveIssue = activeIssues.Contains(a.Id)
                }).ToList()
            };
            
            return Ok(layoutDto);
        }
    }    
}
