using Coworking.Shared.Enums;
using Coworking.Shared.Models;
using Coworking.Shared.Models.DTOs.Building;
using Coworking.Shared.Models.DTOs.Rest;
using CoworkingAPI.Features.AdministrativeHierarchy;
using CoworkingAPI.Features.BuildingTopology;
using CoworkingAPI.Tests.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace CoworkingAPI.Tests.Features.AdministrativeHierarchy;

public class CreateHierarchyCommandHandlersTests : IDisposable
{
    private readonly SqliteTestDatabase db = new();

    public void Dispose() => db.Dispose();

    [Fact]
    public async Task CreateBuilding_PersistsBuildingAndReturnsId()
    {
        Guid id;
        using (var context = db.CreateContext())
        {
            id = await new CreateBuildingCommandHandler(context)
                .ExecuteAsync(new CreateBuildingRequest { Name = "Urban 165" });
        }

        Assert.NotEqual(Guid.Empty, id);
        using var verify = db.CreateContext();
        var building = await verify.Buildings.SingleAsync();
        Assert.Equal(id, building.Id);
        Assert.Equal("Urban 165", building.Name);
    }

    [Fact]
    public async Task CreateFloor_UnknownBuilding_ReturnsNull()
    {
        using var context = db.CreateContext();
        var id = await new CreateFloorCommandHandler(context)
            .ExecuteAsync(new CreateFloorRequest { BuildingId = Guid.NewGuid(), FloorNumber = 2 });

        Assert.Null(id);
    }

    [Fact]
    public async Task CreateFloor_ExistingBuilding_PersistsFloorAndReturnsId()
    {
        SeededTopology t;
        using (var seed = db.CreateContext()) t = TestData.SeedTopology(seed);

        Guid? floorId;
        using (var context = db.CreateContext())
        {
            floorId = await new CreateFloorCommandHandler(context)
                .ExecuteAsync(new CreateFloorRequest { BuildingId = t.BuildingId, FloorNumber = 5 });
        }

        Assert.NotNull(floorId);
        using var verify = db.CreateContext();
        var floor = await verify.Floors.SingleAsync(f => f.Id == floorId);
        Assert.Equal(5, floor.FloorNumber);
        Assert.Equal(t.BuildingId, floor.BuildingId);
    }

    [Fact]
    public async Task CreateRestroom_UnknownFloor_ReturnsNull()
    {
        using var context = db.CreateContext();
        var id = await new CreateRestroomCommandHandler(context)
            .ExecuteAsync(new CreateRestroomRequest { FloorId = Guid.NewGuid(), Identifier = "Sur" });

        Assert.Null(id);
    }

    [Fact]
    public async Task CreateRestroom_ExistingFloor_PersistsWithDefault15x15Grid()
    {
        SeededTopology t;
        using (var seed = db.CreateContext()) t = TestData.SeedTopology(seed);

        Guid? restroomId;
        using (var context = db.CreateContext())
        {
            restroomId = await new CreateRestroomCommandHandler(context)
                .ExecuteAsync(new CreateRestroomRequest { FloorId = t.FloorId, Identifier = "Mujeres" });
        }

        Assert.NotNull(restroomId);
        using var verify = db.CreateContext();
        var restroom = await verify.Restrooms.SingleAsync(r => r.Id == restroomId);
        Assert.Equal("Mujeres", restroom.Identifier);
        Assert.Equal((short)15, restroom.GridWidth);
        Assert.Equal((short)15, restroom.GridHeight);
    }
}

public class DeleteBuildingCommandHandlerTests : IDisposable
{
    private readonly SqliteTestDatabase db = new();
    private readonly Mock<ITopologyCache> cache = new();

    public void Dispose() => db.Dispose();

    [Fact]
    public async Task ExecuteAsync_UnknownBuilding_ReturnsFalse()
    {
        using var context = db.CreateContext();
        var result = await new DeleteBuildingCommandHandler(context, cache.Object).ExecuteAsync(Guid.NewGuid());

        Assert.False(result);
        cache.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ExecuteAsync_EmptyBuilding_SoftDeletesBuildingAndPersistsTransaction()
    {
        Guid buildingId;
        using (var seed = db.CreateContext())
        {
            var b = new Building { Id = Guid.NewGuid(), Name = "Vacío" };
            seed.Buildings.Add(b);
            seed.SaveChanges();
            buildingId = b.Id;
        }

        bool result;
        using (var context = db.CreateContext())
        {
            result = await new DeleteBuildingCommandHandler(context, cache.Object).ExecuteAsync(buildingId);
        }

        Assert.True(result);
        using var verify = db.CreateContext();
        Assert.Empty(await verify.Buildings.ToListAsync());
        Assert.True((await verify.Buildings.IgnoreQueryFilters().SingleAsync(b => b.Id == buildingId)).IsDeleted);
    }

    [Fact]
    public async Task ExecuteAsync_BuildingWithFullHierarchyAndReports_SoftDeletesCascadeAndEvictsCache()
    {
        SeededTopology t;
        using (var seed = db.CreateContext())
        {
            t = TestData.SeedTopology(seed);
            TestData.AddReport(seed, t.ToiletId, ReportStatus.Generated);
        }

        bool result;
        using (var context = db.CreateContext())
        {
            result = await new DeleteBuildingCommandHandler(context, cache.Object).ExecuteAsync(t.BuildingId);
        }

        Assert.True(result);
        cache.Verify(c => c.RemoveAsset(t.ToiletId), Times.Once);
        cache.Verify(c => c.RemoveAsset(t.SinkId), Times.Once);
        cache.Verify(c => c.RemoveAsset(t.HandDryerId), Times.Once);

        // Verify in a brand-new context (fresh connection/session state) that the transaction committed
        using var verify = db.CreateContext();
        Assert.Empty(await verify.Buildings.ToListAsync());
        Assert.Empty(await verify.Floors.ToListAsync());
        Assert.Empty(await verify.Restrooms.ToListAsync());
        Assert.Empty(await verify.RestroomAssets.ToListAsync());
        Assert.Empty(await verify.IssueReports.ToListAsync());
    }
}

public class GetHierarchyQueryHandlerTests : IDisposable
{
    private readonly SqliteTestDatabase db = new();

    public void Dispose() => db.Dispose();

    [Fact]
    public async Task ExecuteAsync_SuperAdmin_SeesAllBuildingsWithFloorsAndRestrooms()
    {
        using (var seed = db.CreateContext())
        {
            TestData.SeedTopology(seed, "Norte", 2, "Caballeros");
            TestData.SeedTopology(seed, "Sur", 1, "Damas");
        }

        using var context = db.CreateContext();
        var result = await new GetHierarchyQueryHandler(context, TestData.AccessorFor("root", "SuperAdmin")).ExecuteAsync();

        Assert.Equal(2, result.Count);
        var norte = result.Single(b => b.Name == "Norte");
        var floor = Assert.Single(norte.Floors);
        Assert.Equal(2, floor.FloorNumber);
        Assert.Equal("Caballeros", Assert.Single(floor.Restrooms).Identifier);
    }

    [Fact]
    public async Task ExecuteAsync_Admin_OnlySeesAssignedBuildings()
    {
        string adminId;
        using (var seed = db.CreateContext())
        {
            var a = TestData.SeedTopology(seed, "Permitido");
            TestData.SeedTopology(seed, "Restringido");
            adminId = TestData.AddUser(seed, "admin@test.com", a.BuildingId).Id;
        }

        using var context = db.CreateContext();
        var result = await new GetHierarchyQueryHandler(context, TestData.AccessorFor(adminId, "Admin")).ExecuteAsync();

        var building = Assert.Single(result);
        Assert.Equal("Permitido", building.Name);
    }

    [Fact]
    public async Task ExecuteAsync_ExcludesSoftDeletedChildren()
    {
        using (var seed = db.CreateContext())
        {
            var t = TestData.SeedTopology(seed, "Edificio", 1, "Activo");
            var deletedRestroom = new Restroom { Id = Guid.NewGuid(), FloorId = t.FloorId, Identifier = "Borrado", IsDeleted = true };
            seed.Restrooms.Add(deletedRestroom);
            seed.SaveChanges();
        }

        using var context = db.CreateContext();
        var result = await new GetHierarchyQueryHandler(context, TestData.AccessorFor("root", "SuperAdmin")).ExecuteAsync();

        var restroom = Assert.Single(result.Single().Floors.Single().Restrooms);
        Assert.Equal("Activo", restroom.Identifier);
    }
}

public class HierarchyControllerTests : IDisposable
{
    private readonly SqliteTestDatabase db = new();

    public void Dispose() => db.Dispose();

    private HierarchyController CreateController(CoworkingAPI.Data.AppDbContext context, ITopologyCache? cache = null)
        => new(
            new CreateBuildingCommandHandler(context),
            new DeleteBuildingCommandHandler(context, cache ?? Mock.Of<ITopologyCache>()),
            new CreateFloorCommandHandler(context),
            new CreateRestroomCommandHandler(context),
            new GetHierarchyQueryHandler(context, TestData.AccessorFor("root", "SuperAdmin")),
            new GetUsersWithBuildingsQueryHandler(context),
            new UpdateBuildingUsersCommandHandler(context));

    [Fact]
    public async Task CreateFloor_MissingBuilding_ReturnsNotFound()
    {
        using var context = db.CreateContext();
        var response = await CreateController(context).CreateFloor(new CreateFloorRequest { BuildingId = Guid.NewGuid(), FloorNumber = 1 });
        Assert.IsType<NotFoundObjectResult>(response);
    }

    [Fact]
    public async Task CreateRestroom_MissingFloor_ReturnsNotFound()
    {
        using var context = db.CreateContext();
        var response = await CreateController(context).CreateRestroom(new CreateRestroomRequest { FloorId = Guid.NewGuid(), Identifier = "B" });
        Assert.IsType<NotFoundObjectResult>(response);
    }

    [Fact]
    public async Task DeleteBuilding_MissingBuilding_ReturnsNotFound()
    {
        using var context = db.CreateContext();
        var response = await CreateController(context).DeleteBuilding(Guid.NewGuid());
        Assert.IsType<NotFoundResult>(response);
    }

    [Fact]
    public async Task GetHierarchy_ReturnsOkWithBuildings()
    {
        using (var seed = db.CreateContext()) TestData.SeedTopology(seed, "Central");

        using var context = db.CreateContext();
        var actionResult = await CreateController(context).GetHierarchy();

        var ok = Assert.IsType<OkObjectResult>(actionResult.Result);
        var buildings = Assert.IsType<List<BuildingDto>>(ok.Value);
        Assert.Single(buildings);
    }

    [Fact]
    public async Task UpdateBuildingUsers_MissingBuilding_ReturnsNotFound()
    {
        using var context = db.CreateContext();
        var response = await CreateController(context)
            .UpdateBuildingUsers(Guid.NewGuid(), new UpdateBuildingUsersRequest { UserIds = { "u1" } });

        Assert.IsType<NotFoundObjectResult>(response);
    }

    [Fact]
    public async Task UpdateBuildingUsers_AssignsAndReplacesUsers_AndGetUsersWithBuildingsReflectsIt()
    {
        SeededTopology t;
        using (var seed = db.CreateContext())
        {
            t = TestData.SeedTopology(seed, "Torre A");
            var roleAdmin = new Microsoft.AspNetCore.Identity.IdentityRole("Admin") { Id = "r-admin", NormalizedName = "ADMIN" };
            seed.Roles.Add(roleAdmin);
            seed.Users.Add(new CoworkingAPI.Data.ApplicationUser { Id = "u1", Email = "admin1@test.com", UserName = "admin1@test.com" });
            seed.Users.Add(new CoworkingAPI.Data.ApplicationUser { Id = "u2", Email = "admin2@test.com", UserName = "admin2@test.com" });
            seed.UserRoles.Add(new Microsoft.AspNetCore.Identity.IdentityUserRole<string> { UserId = "u1", RoleId = "r-admin" });
            seed.UserRoles.Add(new Microsoft.AspNetCore.Identity.IdentityUserRole<string> { UserId = "u2", RoleId = "r-admin" });
            seed.UserBuildings.Add(new CoworkingAPI.Data.UserBuilding { UserId = "u1", BuildingId = t.BuildingId });
            seed.SaveChanges();
        }

        using (var context = db.CreateContext())
        {
            var updateResult = await CreateController(context)
                .UpdateBuildingUsers(t.BuildingId, new UpdateBuildingUsersRequest { UserIds = { "u2", "non-existent" } });
            Assert.IsType<OkResult>(updateResult);
        }

        using (var verify = db.CreateContext())
        {
            var usersResult = await CreateController(verify).GetUsersWithBuildings();
            var ok = Assert.IsType<OkObjectResult>(usersResult.Result);
            var users = Assert.IsType<List<UserBuildingSummaryDto>>(ok.Value);

            var u1 = Assert.Single(users, u => u.UserId == "u1");
            var u2 = Assert.Single(users, u => u.UserId == "u2");
            Assert.Empty(u1.AssignedBuildingIds);
            Assert.Contains(t.BuildingId, u2.AssignedBuildingIds);
            Assert.Contains("Admin", u2.Roles);
        }
    }
}
