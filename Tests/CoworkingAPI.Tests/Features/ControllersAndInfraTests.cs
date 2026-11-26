using System.Security.Claims;
using Coworking.Shared.Enums;
using Coworking.Shared.Models;
using Coworking.Shared.Models.DTOs;
using Coworking.Shared.Models.DTOs.Building;
using Coworking.Shared.Models.DTOs.Rest;
using Coworking.Shared.Models.DTOs.Telemetry;
using Coworking.Shared.Models.Interfaces;
using CoworkingAPI.Data;
using CoworkingAPI.Features.Auth;
using CoworkingAPI.Features.BuildingTopology;
using CoworkingAPI.Features.BuildingTopology.SyncTopology;
using CoworkingAPI.Features.IssueReports;
using CoworkingAPI.Hubs;
using CoworkingAPI.Infrastructure;
using CoworkingAPI.Middleware;
using CoworkingAPI.Tests.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace CoworkingAPI.Tests.Features;

public class IssueReportsControllerTests : IDisposable
{
    private readonly SqliteTestDatabase db = new();
    private readonly Mock<IAlertPublisher> publisher = new();
    private readonly Mock<ITopologyCache> cache = new();

    public void Dispose() => db.Dispose();

    private IssueReportsController CreateController(AppDbContext context)
        => new(
            new CreateIssueReportCommandHandler(context, publisher.Object),
            new GetActiveIssuesQueryHandler(context, TestData.AccessorFor("root", "SuperAdmin")),
            new GetResolvedIssuesDashboardQueryHandler(context, TestData.AccessorFor("root", "SuperAdmin")),
            new AttendIssueReportCommandHandler(context, publisher.Object),
            new ResolveIssueReportCommandHandler(context, publisher.Object),
            cache.Object);

    [Fact]
    public async Task Create_AssetNotInTopologyCache_ReturnsBadRequest()
    {
        var assetId = Guid.NewGuid();
        cache.Setup(c => c.IsValidAsset(assetId)).Returns(false);

        using var context = db.CreateContext();
        var result = await CreateController(context).Create(new CreateIssueReportRequest
        {
            RestroomAssetId = assetId,
            IssueDescription = "Fuga",
            ReportingUserId = "u1"
        });

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Create_BlankDescription_ReturnsBadRequest()
    {
        var assetId = Guid.NewGuid();
        cache.Setup(c => c.IsValidAsset(assetId)).Returns(true);

        using var context = db.CreateContext();
        var result = await CreateController(context).Create(new CreateIssueReportRequest
        {
            RestroomAssetId = assetId,
            IssueDescription = "   ",
            ReportingUserId = "u1"
        });

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Create_DuplicateActiveIssue_ReturnsConflict()
    {
        SeededTopology t;
        using (var seed = db.CreateContext())
        {
            t = TestData.SeedTopology(seed);
            TestData.AddReport(seed, t.ToiletId);
        }
        cache.Setup(c => c.IsValidAsset(t.ToiletId)).Returns(true);

        using var context = db.CreateContext();
        var result = await CreateController(context).Create(new CreateIssueReportRequest
        {
            RestroomAssetId = t.ToiletId,
            IssueDescription = "Tapado",
            ReportingUserId = "u1"
        });

        Assert.IsType<ConflictObjectResult>(result);
    }

    [Fact]
    public async Task Create_ValidRequest_ReturnsOk()
    {
        SeededTopology t;
        using (var seed = db.CreateContext()) t = TestData.SeedTopology(seed);
        cache.Setup(c => c.IsValidAsset(t.ToiletId)).Returns(true);

        using var context = db.CreateContext();
        var result = await CreateController(context).Create(new CreateIssueReportRequest
        {
            RestroomAssetId = t.ToiletId,
            IssueDescription = "Tapado",
            ReportingUserId = "u1"
        });

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Attend_UnknownId_ReturnsNotFound()
    {
        using var context = db.CreateContext();
        var result = await CreateController(context).Attend(Guid.NewGuid());
        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task Resolve_UnknownId_ReturnsNotFound()
    {
        using var context = db.CreateContext();
        var result = await CreateController(context).Resolve(Guid.NewGuid());
        Assert.IsType<NotFoundObjectResult>(result);
    }
}

public class TopologyControllerTests : IDisposable
{
    private readonly SqliteTestDatabase db = new();
    private readonly Mock<ITopologyCache> cache = new();

    public void Dispose() => db.Dispose();

    private TopologyController CreateController(AppDbContext context)
        => new(
            new SyncRestroomTopologyCommandHandler(context, cache.Object),
            new GetRestroomTopologyQueryHandler(context));

    [Fact]
    public async Task GetTopology_UnknownId_ReturnsNotFound()
    {
        using var context = db.CreateContext();
        Assert.IsType<NotFoundResult>(await CreateController(context).GetTopology(Guid.NewGuid()));
    }

    [Fact]
    public async Task GetTopology_ExistingRestroom_ReturnsOkWithLayout()
    {
        SeededTopology t;
        using (var seed = db.CreateContext()) t = TestData.SeedTopology(seed);

        using var context = db.CreateContext();
        var result = await CreateController(context).GetTopology(t.RestroomId);

        var ok = Assert.IsType<OkObjectResult>(result);
        var layout = Assert.IsType<RestroomLayoutDto>(ok.Value);
        Assert.Equal(t.RestroomId, layout.RestroomId);
    }

    [Fact]
    public async Task SyncTopology_CollisionError_ReturnsBadRequest()
    {
        SeededTopology t;
        using (var seed = db.CreateContext()) t = TestData.SeedTopology(seed);

        using var context = db.CreateContext();
        var result = await CreateController(context).SyncTopology(new SyncRestroomTopologyRequest
        {
            RestroomId = t.RestroomId,
            GridWidth = 10,
            GridHeight = 10,
            Assets =
            {
                new TopologyAssetDto { Type = AssetType.Toilet, PositionX = 1, PositionY = 1 },
                new TopologyAssetDto { Type = AssetType.Sink, PositionX = 1, PositionY = 1 }
            }
        });

        Assert.IsType<BadRequestObjectResult>(result);
    }
}

public class AuthControllerTests
{
    private static Mock<UserManager<ApplicationUser>> CreateUserManagerMock()
    {
        var store = new Mock<IUserStore<ApplicationUser>>();
        return new Mock<UserManager<ApplicationUser>>(store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
    }

    [Fact]
    public async Task GetMe_NoNameIdentifierClaim_ReturnsUnauthorized()
    {
        var controller = new AuthController(CreateUserManagerMock().Object)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        Assert.IsType<UnauthorizedResult>(await controller.GetMe());
    }

    [Fact]
    public async Task GetMe_UserNotFoundInStore_ReturnsUnauthorized()
    {
        var userManager = CreateUserManagerMock();
        userManager.Setup(m => m.FindByIdAsync("u-missing")).ReturnsAsync((ApplicationUser?)null);

        var controller = new AuthController(userManager.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = TestData.Principal("u-missing", "Admin") }
            }
        };

        Assert.IsType<UnauthorizedResult>(await controller.GetMe());
    }

    [Fact]
    public async Task GetMe_ValidUser_ReturnsEmailAndRoles()
    {
        var user = new ApplicationUser { Id = "u-1", Email = "admin@coworking.com" };
        var userManager = CreateUserManagerMock();
        userManager.Setup(m => m.FindByIdAsync("u-1")).ReturnsAsync(user);
        userManager.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(new List<string> { "Admin" });

        var controller = new AuthController(userManager.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = TestData.Principal("u-1", "Admin") }
            }
        };

        var ok = Assert.IsType<OkObjectResult>(await controller.GetMe());
        var emailProp = ok.Value!.GetType().GetProperty("Email")!.GetValue(ok.Value);
        var rolesProp = ok.Value!.GetType().GetProperty("Roles")!.GetValue(ok.Value) as IList<string>;
        Assert.Equal("admin@coworking.com", emailProp);
        Assert.Equal(new[] { "Admin" }, rolesProp);
    }
}

public class InfrastructureTests : IDisposable
{
    private readonly SqliteTestDatabase db = new();

    public void Dispose() => db.Dispose();

    [Fact]
    public async Task SignalRQueryStringAuthMiddleware_InjectsBearerTokenForHubPath()
    {
        var called = false;
        var middleware = new SignalRQueryStringAuthMiddleware(_ => { called = true; return Task.CompletedTask; });
        var context = new DefaultHttpContext();
        context.Request.Path = "/hubs/alert-telemetry/negotiate";
        context.Request.QueryString = new QueryString("?access_token=abc.def.ghi");

        await middleware.InvokeAsync(context);

        Assert.True(called);
        Assert.Equal("Bearer abc.def.ghi", context.Request.Headers.Authorization.ToString());
    }

    [Fact]
    public async Task SignalRQueryStringAuthMiddleware_DoesNotOverwriteExistingHeaderOrTouchOtherPaths()
    {
        var middleware = new SignalRQueryStringAuthMiddleware(_ => Task.CompletedTask);

        var otherPath = new DefaultHttpContext();
        otherPath.Request.Path = "/api/IssueReports";
        otherPath.Request.QueryString = new QueryString("?access_token=secret");
        await middleware.InvokeAsync(otherPath);
        Assert.False(otherPath.Request.Headers.ContainsKey("Authorization"));

        var existingHeader = new DefaultHttpContext();
        existingHeader.Request.Path = "/hubs/alert-telemetry";
        existingHeader.Request.QueryString = new QueryString("?access_token=new");
        existingHeader.Request.Headers.Authorization = "Bearer original";
        await middleware.InvokeAsync(existingHeader);
        Assert.Equal("Bearer original", existingHeader.Request.Headers.Authorization.ToString());
    }

    [Fact]
    public async Task SignalRAlertPublisher_BroadcastsToBuildingGroup()
    {
        var clientProxy = new Mock<IClientProxy>();
        var hubClients = new Mock<IHubClients>();
        var buildingId = Guid.NewGuid();
        hubClients.Setup(c => c.Group($"Building_{buildingId}")).Returns(clientProxy.Object);

        var hubContext = new Mock<IHubContext<AlertTelemetryHub>>();
        hubContext.Setup(h => h.Clients).Returns(hubClients.Object);

        var payload = new ReportStatusUpdateTelemetry { ReportId = Guid.NewGuid(), Status = ReportStatus.InProgress };
        await new SignalRAlertPublisher(hubContext.Object).BroadcastStatusUpdateAsync(payload, buildingId);

        clientProxy.Verify(p => p.SendCoreAsync(
            "OnReportStatusUpdated",
            It.Is<object?[]>(args => args.Length == 1 && Equals(args[0], payload)),
            default), Times.Once);
    }

    [Fact]
    public async Task AppDbContext_SaveChangesAsync_ConvertsRemoveIntoSoftDelete()
    {
        Guid buildingId;
        using (var seed = db.CreateContext())
        {
            var b = new Building { Id = Guid.NewGuid(), Name = "SoftDeleteTest" };
            seed.Buildings.Add(b);
            await seed.SaveChangesAsync();
            buildingId = b.Id;
        }

        using (var deleteCtx = db.CreateContext())
        {
            var b = await deleteCtx.Buildings.SingleAsync(x => x.Id == buildingId);
            deleteCtx.Buildings.Remove(b);
            await deleteCtx.SaveChangesAsync();
        }

        using var verify = db.CreateContext();
        Assert.Empty(await verify.Buildings.ToListAsync());
        var softDeleted = await verify.Buildings.IgnoreQueryFilters().SingleAsync(x => x.Id == buildingId);
        Assert.True(softDeleted.IsDeleted);
    }
}
