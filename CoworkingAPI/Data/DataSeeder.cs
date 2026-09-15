using Coworking.Shared.Enums;
using Coworking.Shared.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CoworkingAPI.Data;

public static class DataSeeder
{
    public static async Task SeedRolesAndSuperAdminAsync(IServiceProvider serviceProvider)
    {
        var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var dbContext = serviceProvider.GetRequiredService<AppDbContext>();

        string[] roles = { "SuperAdmin", "Admin", "CleaningStaff" };
        
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        // Seed SuperAdmin
        var superAdminEmail = "superadmin@coworking.com";
        var superAdminUser = await userManager.FindByEmailAsync(superAdminEmail);
        
        if (superAdminUser == null)
        {
            superAdminUser = new ApplicationUser
            {
                UserName = superAdminEmail,
                Email = superAdminEmail,
                EmailConfirmed = true
            };
            
            var result = await userManager.CreateAsync(superAdminUser, "Admin123*");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(superAdminUser, "SuperAdmin");
            }
        }

        // Seed Admin Mock
        var adminEmail = "admin@coworking.com";
        var adminUser = await userManager.FindByEmailAsync(adminEmail);
        
        if (adminUser == null)
        {
            adminUser = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true
            };
            
            var result = await userManager.CreateAsync(adminUser, "Admin123*");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, "Admin");
            }
        }

        // Create Mock Building if not exists
        var buildingName = "Edificio Norte (Mock)";
        var mockBuilding = await dbContext.Buildings.FirstOrDefaultAsync(b => b.Name == buildingName);
        
        if (mockBuilding == null)
        {
            mockBuilding = new Building { Id = Guid.NewGuid(), Name = buildingName };
            dbContext.Buildings.Add(mockBuilding);

            var mockFloor = new Floor { Id = Guid.NewGuid(), BuildingId = mockBuilding.Id, FloorNumber = 1 };
            dbContext.Floors.Add(mockFloor);

            var mockRestroom = new Restroom 
            { 
                Id = Guid.NewGuid(), 
                FloorId = mockFloor.Id, 
                Identifier = "Baño Principal Norte",
                GridWidth = 5,
                GridHeight = 5
            };
            dbContext.Restrooms.Add(mockRestroom);

            var mockAsset = new Toilet 
            { 
                Id = Guid.NewGuid(), 
                RestroomId = mockRestroom.Id, 
                PositionX = 2, 
                PositionY = 2 
            };
            dbContext.RestroomAssets.Add(mockAsset);

            await dbContext.SaveChangesAsync();
        }

        // Assign building to Admin User
        if (adminUser != null && mockBuilding != null)
        {
            var userBuildingExists = await dbContext.UserBuildings
                .AnyAsync(ub => ub.UserId == adminUser.Id && ub.BuildingId == mockBuilding.Id);

            if (!userBuildingExists)
            {
                dbContext.UserBuildings.Add(new UserBuilding 
                { 
                    UserId = adminUser.Id, 
                    BuildingId = mockBuilding.Id 
                });
                await dbContext.SaveChangesAsync();
            }
        }
    }
}
