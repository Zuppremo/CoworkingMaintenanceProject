using Coworking.Shared.Models;
using CoworkingAPI.Data;
using Microsoft.AspNetCore.Mvc;

namespace CoworkingAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SeedController : ControllerBase
    {
        private readonly AppDbContext context;

        public SeedController(AppDbContext context)
        {
            this.context = context;
        }

        [HttpPost]
        public async Task<IActionResult> SeedDatabase()
        {
            // 1. Build the objects from the bottom up and nest them via Navigation Properties
            var toilet = new Toilet 
            { 
                PositionX = 2, 
                PositionY = 5, 
                IsDamaged = false,
            };

            var restroom = new Restroom 
            { 
                Identifier = "First Floor Main Restroom", 
                GridWidth = 10, 
                GridHeight = 10,
                Assets = new List<RestroomAsset> { toilet } // Nested here
            };

            var floor = new Floor 
            { 
                FloorNumber = 1,
                Restrooms = new List<Restroom> { restroom } // Nested here
            };

            var building = new Building 
            { 
                Name = "Coworking Central Hub",
                Floors = new List<Floor> { floor } // Nested here
            };

            // 2. Add ONLY the root object. EF Core cascades the inserts automatically.
            context.Buildings.Add(building);
    
            await context.SaveChangesAsync();

            return Ok(new 
            { 
                Message = "Database Seeded Successfully!", 
                ValidAssetIdToTest = toilet.Id 
            });
        }
    }
}
