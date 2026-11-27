using Coworking.Shared.Enums;
using Coworking.Shared.Models;
using Coworking.Shared.Models.DTOs.Rest;
using CoworkingAPI.Data;
using CoworkingAPI.Features.BuildingTopology;
using CoworkingAPI.Features.BuildingTopology.SyncTopology;
using CoworkingAPI.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace CoworkingAPI.Tests.Features.BuildingTopology;

public class TopologyCacheTests
{
    [Fact]
    public void IsValidAsset_UnknownId_ReturnsFalse()
    {
        Assert.False(new TopologyCache().IsValidAsset(Guid.NewGuid()));
    }

    [Fact]
    public void AddAsset_ThenIsValid_ReturnsTrue_AndAddingTwiceIsIdempotent()
    {
        var cache = new TopologyCache();
        var id = Guid.NewGuid();

        cache.AddAsset(id);
        cache.AddAsset(id);

        Assert.True(cache.IsValidAsset(id));
        cache.RemoveAsset(id);
        Assert.False(cache.IsValidAsset(id));
    }

    [Fact]
    public void RemoveAsset_UnknownId_DoesNotThrow()
    {
        var cache = new TopologyCache();
        var ex = Record.Exception(() => cache.RemoveAsset(Guid.NewGuid()));
        Assert.Null(ex);
    }

    [Fact]
    public void Seed_ReplacesPreviousContent()
    {
        var cache = new TopologyCache();
        var old = Guid.NewGuid();
        var fresh = Guid.NewGuid();
        cache.AddAsset(old);

        cache.Seed(new[] { fresh });

        Assert.False(cache.IsValidAsset(old));
        Assert.True(cache.IsValidAsset(fresh));
    }

    [Fact]
    public async Task ConcurrentAddsAndReads_AreThreadSafe()
    {
        var cache = new TopologyCache();
        var ids = Enumerable.Range(0, 2000).Select(_ => Guid.NewGuid()).ToArray();

        await Task.WhenAll(ids.Select(id => Task.Run(() => cache.AddAsset(id))));

        Assert.All(ids, id => Assert.True(cache.IsValidAsset(id)));
    }
}

public class TopologyCacheWarmerTests : IDisposable
{
    private readonly SqliteTestDatabase db = new();

    public void Dispose() => db.Dispose();

    [Fact]
    public async Task StartAsync_SeedsCacheWithNonDeletedAssetsOnly()
    {
        SeededTopology t;
        using (var seed = db.CreateContext())
        {
            t = TestData.SeedTopology(seed);
            seed.RestroomAssets.Single(a => a.Id == t.SinkId).IsDeleted = true;
            seed.SaveChanges();
        }

        var services = new ServiceCollection();
        services.AddScoped<AppDbContext>(_ => db.CreateContext());
        using var provider = services.BuildServiceProvider();
        var cache = new TopologyCache();

        await new TopologyCacheWarmer(provider, cache).StartAsync(CancellationToken.None);

        Assert.True(cache.IsValidAsset(t.ToiletId));
        Assert.True(cache.IsValidAsset(t.HandDryerId));
        Assert.False(cache.IsValidAsset(t.SinkId));
    }
}

public class SyncRestroomTopologyCommandHandlerTests : IDisposable
{
    private readonly SqliteTestDatabase db = new();
    private readonly Mock<ITopologyCache> cache = new();

    public void Dispose() => db.Dispose();

    private Guid SeedEmptyRestroom(short width = 10, short height = 10)
    {
        using var seed = db.CreateContext();
        var building = new Building { Id = Guid.NewGuid(), Name = "B" };
        var floor = new Floor { Id = Guid.NewGuid(), BuildingId = building.Id, FloorNumber = 1 };
        var restroom = new Restroom { Id = Guid.NewGuid(), FloorId = floor.Id, Identifier = "R", GridWidth = width, GridHeight = height };
        seed.AddRange(building, floor, restroom);
        seed.SaveChanges();
        return restroom.Id;
    }

    private async Task<string?> Sync(SyncRestroomTopologyRequest request)
    {
        using var context = db.CreateContext();
        return await new SyncRestroomTopologyCommandHandler(context, cache.Object).ExecuteAsync(request);
    }

    [Fact]
    public async Task ExecuteAsync_UnknownRestroom_ReturnsError()
    {
        var error = await Sync(new SyncRestroomTopologyRequest { RestroomId = Guid.NewGuid(), GridWidth = 10, GridHeight = 10 });
        Assert.Equal("Restroom not found.", error);
    }

    [Fact]
    public async Task ExecuteAsync_TwoFloorAssetsOnSameCell_ReturnsCollisionError()
    {
        var restroomId = SeedEmptyRestroom();
        var error = await Sync(new SyncRestroomTopologyRequest
        {
            RestroomId = restroomId, GridWidth = 10, GridHeight = 10,
            Assets =
            {
                new TopologyAssetDto { Type = AssetType.Toilet, PositionX = 3, PositionY = 3 },
                new TopologyAssetDto { Type = AssetType.Sink, PositionX = 3, PositionY = 3 }
            }
        });

        Assert.StartsWith("Collision detected", error);
        using var verify = db.CreateContext();
        Assert.Empty(verify.RestroomAssets);
    }

    [Fact]
    public async Task ExecuteAsync_TwoWallAssetsOnSameCell_ReturnsCollisionError()
    {
        var restroomId = SeedEmptyRestroom();
        var error = await Sync(new SyncRestroomTopologyRequest
        {
            RestroomId = restroomId, GridWidth = 10, GridHeight = 10,
            Assets =
            {
                new TopologyAssetDto { Type = AssetType.HandDryer, PositionX = 0, PositionY = 0 },
                new TopologyAssetDto { Type = AssetType.PaperDispenser, PositionX = 0, PositionY = 0 }
            }
        });

        Assert.StartsWith("Collision detected", error);
    }

    [Fact]
    public async Task ExecuteAsync_FloorAndWallAssetOnSameCell_IsAllowed()
    {
        var restroomId = SeedEmptyRestroom();
        var error = await Sync(new SyncRestroomTopologyRequest
        {
            RestroomId = restroomId, GridWidth = 10, GridHeight = 10,
            Assets =
            {
                new TopologyAssetDto { Type = AssetType.Sink, PositionX = 4, PositionY = 4 },
                new TopologyAssetDto { Type = AssetType.HandDryer, PositionX = 4, PositionY = 4 }
            }
        });

        Assert.Null(error);
        using var verify = db.CreateContext();
        Assert.Equal(2, verify.RestroomAssets.Count());
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(0, -1)]
    [InlineData(10, 0)] // x == width is outside (0-based)
    [InlineData(0, 8)]  // y == height is outside
    public async Task ExecuteAsync_AssetOutOfBounds_ReturnsError(short x, short y)
    {
        var restroomId = SeedEmptyRestroom();
        var error = await Sync(new SyncRestroomTopologyRequest
        {
            RestroomId = restroomId, GridWidth = 10, GridHeight = 8,
            Assets = { new TopologyAssetDto { Type = AssetType.Toilet, PositionX = x, PositionY = y } }
        });

        Assert.Equal("Assets cannot be placed outside the defined grid boundaries.", error);
    }

    [Fact]
    public async Task ExecuteAsync_NewAssets_ArePersistedWithCorrectSubtypeAndAddedToCache()
    {
        var restroomId = SeedEmptyRestroom();
        var clientId = Guid.NewGuid();

        var error = await Sync(new SyncRestroomTopologyRequest
        {
            RestroomId = restroomId, GridWidth = 12, GridHeight = 9,
            Assets =
            {
                new TopologyAssetDto { Id = clientId, Type = AssetType.PaperDispenser, PositionX = 1, PositionY = 2 },
                new TopologyAssetDto { Type = AssetType.Toilet, PositionX = 5, PositionY = 5 }
            }
        });

        Assert.Null(error);
        using var verify = db.CreateContext();
        var restroom = verify.Restrooms.Include(r => r.Assets).Single();
        Assert.Equal((short)12, restroom.GridWidth);
        Assert.Equal((short)9, restroom.GridHeight);
        Assert.Equal(2, restroom.Assets.Count);
        var dispenser = Assert.IsType<PaperDispenser>(restroom.Assets.Single(a => a.Id == clientId));
        Assert.Equal((short)1, dispenser.PositionX);
        Assert.IsType<Toilet>(restroom.Assets.Single(a => a.Id != clientId));

        foreach (var asset in restroom.Assets)
        {
            cache.Verify(c => c.AddAsset(asset.Id), Times.Once);
        }
    }

    [Fact]
    public async Task ExecuteAsync_ExistingAsset_IsMovedNotDuplicated()
    {
        var restroomId = SeedEmptyRestroom();
        var assetId = Guid.NewGuid();
        await Sync(new SyncRestroomTopologyRequest
        {
            RestroomId = restroomId, GridWidth = 10, GridHeight = 10,
            Assets = { new TopologyAssetDto { Id = assetId, Type = AssetType.Toilet, PositionX = 1, PositionY = 1 } }
        });
        cache.Invocations.Clear();

        var error = await Sync(new SyncRestroomTopologyRequest
        {
            RestroomId = restroomId, GridWidth = 10, GridHeight = 10,
            Assets = { new TopologyAssetDto { Id = assetId, Type = AssetType.Toilet, PositionX = 7, PositionY = 8 } }
        });

        Assert.Null(error);
        using var verify = db.CreateContext();
        var asset = Assert.Single(verify.RestroomAssets);
        Assert.Equal(assetId, asset.Id);
        Assert.Equal(((short)7, (short)8), (asset.PositionX, asset.PositionY));
        cache.Verify(c => c.AddAsset(It.IsAny<Guid>()), Times.Never);
        cache.Verify(c => c.RemoveAsset(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_AssetMissingFromRequest_IsSoftDeletedAndEvictedFromCache()
    {
        var restroomId = SeedEmptyRestroom();
        var keep = Guid.NewGuid();
        var drop = Guid.NewGuid();
        await Sync(new SyncRestroomTopologyRequest
        {
            RestroomId = restroomId, GridWidth = 10, GridHeight = 10,
            Assets =
            {
                new TopologyAssetDto { Id = keep, Type = AssetType.Toilet, PositionX = 1, PositionY = 1 },
                new TopologyAssetDto { Id = drop, Type = AssetType.Sink, PositionX = 2, PositionY = 2 }
            }
        });

        await Sync(new SyncRestroomTopologyRequest
        {
            RestroomId = restroomId, GridWidth = 10, GridHeight = 10,
            Assets = { new TopologyAssetDto { Id = keep, Type = AssetType.Toilet, PositionX = 1, PositionY = 1 } }
        });

        using var verify = db.CreateContext();
        Assert.Equal(keep, Assert.Single(verify.RestroomAssets).Id);
        var deleted = verify.RestroomAssets.IgnoreQueryFilters().Single(a => a.Id == drop);
        Assert.True(deleted.IsDeleted);
        cache.Verify(c => c.RemoveAsset(drop), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_EmptyAssetList_SoftDeletesEverything()
    {
        var restroomId = SeedEmptyRestroom();
        await Sync(new SyncRestroomTopologyRequest
        {
            RestroomId = restroomId, GridWidth = 10, GridHeight = 10,
            Assets = { new TopologyAssetDto { Type = AssetType.Toilet, PositionX = 1, PositionY = 1 } }
        });

        var error = await Sync(new SyncRestroomTopologyRequest { RestroomId = restroomId, GridWidth = 10, GridHeight = 10 });

        Assert.Null(error);
        using var verify = db.CreateContext();
        Assert.Empty(verify.RestroomAssets);
        Assert.Single(verify.RestroomAssets.IgnoreQueryFilters());
    }

    [Fact]
    public async Task ExecuteAsync_ValidationFailure_DoesNotTouchTheCache()
    {
        var restroomId = SeedEmptyRestroom();
        await Sync(new SyncRestroomTopologyRequest
        {
            RestroomId = restroomId, GridWidth = 5, GridHeight = 5,
            Assets = { new TopologyAssetDto { Type = AssetType.Toilet, PositionX = 9, PositionY = 9 } }
        });

        cache.VerifyNoOtherCalls();
    }
}

public class GetRestroomTopologyQueryHandlerTests : IDisposable
{
    private readonly SqliteTestDatabase db = new();

    public void Dispose() => db.Dispose();

    [Fact]
    public async Task ExecuteAsync_UnknownRestroom_ReturnsNull()
    {
        using var context = db.CreateContext();
        Assert.Null(await new GetRestroomTopologyQueryHandler(context).ExecuteAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsLayoutWithAllNonDeletedAssets()
    {
        SeededTopology t;
        using (var seed = db.CreateContext())
        {
            t = TestData.SeedTopology(seed, restroomIdentifier: "Lobby");
            seed.RestroomAssets.Single(a => a.Id == t.SinkId).IsDeleted = true;
            seed.SaveChanges();
        }

        using var context = db.CreateContext();
        var layout = await new GetRestroomTopologyQueryHandler(context).ExecuteAsync(t.RestroomId);

        Assert.NotNull(layout);
        Assert.Equal(t.RestroomId, layout.RestroomId);
        Assert.Equal("Lobby", layout.Identifier);
        Assert.Equal((short)10, layout.GridWidth);
        Assert.Equal((short)10, layout.GridHeight);
        Assert.Equal(new[] { t.ToiletId, t.HandDryerId }.OrderBy(x => x), layout.Assets.Select(a => a.Id).OrderBy(x => x));
        var toilet = layout.Assets.Single(a => a.Id == t.ToiletId);
        Assert.Equal(AssetType.Toilet, toilet.Type);
        Assert.Equal(((short)1, (short)1), (toilet.PositionX, toilet.PositionY));
    }

    [Fact]
    public async Task ExecuteAsync_AssetWithActiveReport_IsFlaggedWithHasActiveIssue()
    {
        // RestroomAssetDto exposes HasActiveIssue, and the legacy RestroomsController filled it,
        // but this handler always returns false, so clients cannot tell which assets are already reported.
        SeededTopology t;
        using (var seed = db.CreateContext())
        {
            t = TestData.SeedTopology(seed);
            TestData.AddReport(seed, t.ToiletId, ReportStatus.InProgress);
            TestData.AddReport(seed, t.SinkId, ReportStatus.Resolved, resolutionDate: TestData.BaseTime.AddHours(1));
        }

        using var context = db.CreateContext();
        var layout = await new GetRestroomTopologyQueryHandler(context).ExecuteAsync(t.RestroomId);

        Assert.True(layout!.Assets.Single(a => a.Id == t.ToiletId).HasActiveIssue);
        Assert.False(layout.Assets.Single(a => a.Id == t.SinkId).HasActiveIssue);
        Assert.False(layout.Assets.Single(a => a.Id == t.HandDryerId).HasActiveIssue);
    }
}
