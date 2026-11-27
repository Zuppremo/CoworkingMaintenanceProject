using Coworking.Shared.Enums;
using CoworkingAPI.Features.IssueReports;
using CoworkingAPI.Tests.Infrastructure;

namespace CoworkingAPI.Tests.Features.IssueReports;

public class GetActiveIssuesQueryHandlerTests : IDisposable
{
    private readonly SqliteTestDatabase db = new();

    public void Dispose() => db.Dispose();

    [Fact]
    public async Task ExecuteAsync_SuperAdmin_SeesActiveIssuesOfAllBuildings()
    {
        using (var seed = db.CreateContext())
        {
            var a = TestData.SeedTopology(seed, "Edificio A");
            var b = TestData.SeedTopology(seed, "Edificio B");
            TestData.AddReport(seed, a.ToiletId);
            TestData.AddReport(seed, b.SinkId, ReportStatus.InProgress);
        }

        using var context = db.CreateContext();
        var handler = new GetActiveIssuesQueryHandler(context, TestData.AccessorFor("root", "SuperAdmin"));

        var result = await handler.ExecuteAsync();

        Assert.Equal(2, result.Count);
        Assert.Contains(result, r => r.BuildingName == "Edificio A");
        Assert.Contains(result, r => r.BuildingName == "Edificio B");
    }

    [Fact]
    public async Task ExecuteAsync_ExcludesResolvedIssues()
    {
        using (var seed = db.CreateContext())
        {
            var t = TestData.SeedTopology(seed);
            TestData.AddReport(seed, t.ToiletId, ReportStatus.Resolved, resolutionDate: TestData.BaseTime.AddHours(1));
            TestData.AddReport(seed, t.SinkId);
        }

        using var context = db.CreateContext();
        var result = await new GetActiveIssuesQueryHandler(context, TestData.AccessorFor("root", "SuperAdmin")).ExecuteAsync();

        Assert.Single(result);
        Assert.Equal(ReportStatus.Generated, result[0].Status);
    }

    [Fact]
    public async Task ExecuteAsync_NonSuperAdmin_OnlySeesAssignedBuildings()
    {
        string adminId;
        using (var seed = db.CreateContext())
        {
            var a = TestData.SeedTopology(seed, "Asignado");
            var b = TestData.SeedTopology(seed, "Ajeno");
            TestData.AddReport(seed, a.ToiletId);
            TestData.AddReport(seed, b.ToiletId);
            adminId = TestData.AddUser(seed, "admin@test.com", a.BuildingId).Id;
        }

        using var context = db.CreateContext();
        var result = await new GetActiveIssuesQueryHandler(context, TestData.AccessorFor(adminId, "Admin")).ExecuteAsync();

        var issue = Assert.Single(result);
        Assert.Equal("Asignado", issue.BuildingName);
    }

    [Fact]
    public async Task ExecuteAsync_UserWithoutAssignedBuildings_GetsEmptyList()
    {
        // Documents current behaviour: a CleaningStaff user that has not been linked to any
        // building (there is no API endpoint to do so) never sees any issue in the mobile app.
        string staffId;
        using (var seed = db.CreateContext())
        {
            var a = TestData.SeedTopology(seed);
            TestData.AddReport(seed, a.ToiletId);
            staffId = TestData.AddUser(seed, "staff@test.com").Id;
        }

        using var context = db.CreateContext();
        var result = await new GetActiveIssuesQueryHandler(context, TestData.AccessorFor(staffId, "CleaningStaff")).ExecuteAsync();

        Assert.Empty(result);
    }

    [Fact]
    public async Task ExecuteAsync_MapsLocationAndAssetDataAndOrdersNewestFirst()
    {
        SeededTopology t;
        using (var seed = db.CreateContext())
        {
            t = TestData.SeedTopology(seed, "Torre", floorNumber: 7, restroomIdentifier: "Norte");
            TestData.AddReport(seed, t.ToiletId, generationDate: TestData.BaseTime, description: "viejo");
            TestData.AddReport(seed, t.HandDryerId, generationDate: TestData.BaseTime.AddHours(2), description: "nuevo");
        }

        using var context = db.CreateContext();
        var result = await new GetActiveIssuesQueryHandler(context, TestData.AccessorFor("root", "SuperAdmin")).ExecuteAsync();

        Assert.Equal(new[] { "nuevo", "viejo" }, result.Select(r => r.IssueDescription));
        var newest = result[0];
        Assert.Equal(t.HandDryerId, newest.RestroomAssetId);
        Assert.Equal(AssetType.HandDryer, newest.AssetType);
        Assert.Equal((short)2, newest.PositionX);
        Assert.Equal((short)1, newest.PositionY);
        Assert.Equal(t.RestroomId, newest.RestroomId);
        Assert.Equal("Norte", newest.RestroomIdentifier);
        Assert.Equal(7, newest.FloorNumber);
        Assert.Equal("Torre", newest.BuildingName);
    }

    [Fact]
    public async Task ExecuteAsync_SoftDeletedAssets_AreExcluded()
    {
        using (var seed = db.CreateContext())
        {
            var t = TestData.SeedTopology(seed);
            TestData.AddReport(seed, t.ToiletId);
            var toilet = seed.RestroomAssets.Single(a => a.Id == t.ToiletId);
            toilet.IsDeleted = true;
            seed.SaveChanges();
        }

        using var context = db.CreateContext();
        var result = await new GetActiveIssuesQueryHandler(context, TestData.AccessorFor("root", "SuperAdmin")).ExecuteAsync();

        Assert.Empty(result);
    }
}

public class GetResolvedIssuesDashboardQueryHandlerTests : IDisposable
{
    private readonly SqliteTestDatabase db = new();

    public void Dispose() => db.Dispose();

    private static readonly DateTimeOffset T0 = TestData.BaseTime;

    private GetResolvedIssuesDashboardQueryHandler SuperAdminHandler(CoworkingAPI.Data.AppDbContext context)
        => new(context, TestData.AccessorFor("root", "SuperAdmin"));

    [Fact]
    public async Task ExecuteAsync_NoData_ReturnsZeroedDashboard()
    {
        using var context = db.CreateContext();
        var dashboard = await SuperAdminHandler(context).ExecuteAsync();

        Assert.Equal(0, dashboard.TotalResolved);
        Assert.Equal(0, dashboard.TotalActive);
        Assert.Equal(0, dashboard.ResolutionRatePercent);
        Assert.Equal(0, dashboard.AvgMttrMinutes);
        Assert.Equal(0, dashboard.AvgMttaMinutes);
        Assert.Empty(dashboard.ResolvedIssues);
        Assert.Empty(dashboard.DailyTrend);
    }

    [Fact]
    public async Task ExecuteAsync_ComputesMttrMttaAndResolutionRate()
    {
        using (var seed = db.CreateContext())
        {
            var t = TestData.SeedTopology(seed);
            // Resolved in 30 min, attended after 10 min (active repair 20)
            TestData.AddReport(seed, t.ToiletId, ReportStatus.Resolved, T0, T0.AddMinutes(10), T0.AddMinutes(30),
                assignedOperatorId: "op-1");
            // Resolved in 60 min, never explicitly attended
            TestData.AddReport(seed, t.SinkId, ReportStatus.Resolved, T0, null, T0.AddMinutes(60));
            // One active issue
            TestData.AddReport(seed, t.HandDryerId, ReportStatus.InProgress, T0, T0.AddMinutes(1));
        }

        using var context = db.CreateContext();
        var d = await SuperAdminHandler(context).ExecuteAsync();

        Assert.Equal(2, d.TotalResolved);
        Assert.Equal(1, d.TotalActive);
        Assert.Equal(66.7, d.ResolutionRatePercent);
        Assert.Equal(45, d.AvgMttrMinutes);
        Assert.Equal(10, d.AvgMttaMinutes);
        Assert.Equal(20, d.AvgActiveRepairMinutes);
        Assert.Equal(30, d.FastestResolutionMinutes);
        Assert.Equal(60, d.SlowestResolutionMinutes);
    }

    [Fact]
    public async Task ExecuteAsync_UnassignedOperator_IsReportedAsSinAsignar()
    {
        using (var seed = db.CreateContext())
        {
            var t = TestData.SeedTopology(seed);
            TestData.AddReport(seed, t.ToiletId, ReportStatus.Resolved, T0, null, T0.AddMinutes(5));
        }

        using var context = db.CreateContext();
        var d = await SuperAdminHandler(context).ExecuteAsync();

        Assert.Equal("Sin asignar", Assert.Single(d.ResolvedIssues).AssignedOperatorId);
        Assert.Equal("Sin asignar", Assert.Single(d.ByOperator).OperatorId);
    }

    [Fact]
    public async Task ExecuteAsync_GroupsByAssetTypeLocationDescriptionDayAndOperator()
    {
        using (var seed = db.CreateContext())
        {
            var t = TestData.SeedTopology(seed, "Torre", 3, "Sur");
            TestData.AddReport(seed, t.ToiletId, ReportStatus.Resolved, T0, null, T0.AddMinutes(10), " Sin papel ", "op-1");
            TestData.AddReport(seed, t.ToiletId, ReportStatus.Resolved, T0.AddDays(1), null, T0.AddDays(1).AddMinutes(20), "Sin papel", "op-1");
            TestData.AddReport(seed, t.SinkId, ReportStatus.Resolved, T0.AddDays(1), null, T0.AddDays(1).AddMinutes(30), "Fuga", "op-2");
        }

        using var context = db.CreateContext();
        var d = await SuperAdminHandler(context).ExecuteAsync();

        var toilet = d.ByAssetType.First();
        Assert.Equal(AssetType.Toilet, toilet.AssetType);
        Assert.Equal(2, toilet.ResolvedCount);
        Assert.Equal(66.7, toilet.Percentage);
        Assert.Equal(15, toilet.AvgMttrMinutes);

        var location = Assert.Single(d.ByLocation);
        Assert.Equal(("Torre", 3, "Sur", 3), (location.BuildingName, location.FloorNumber, location.RestroomIdentifier, location.ResolvedCount));

        var top = d.TopIssueDescriptions.First();
        Assert.Equal("Sin papel", top.Description); // trimmed and merged
        Assert.Equal(2, top.Count);

        Assert.Equal(2, d.DailyTrend.Count);
        Assert.Equal(DateOnly.FromDateTime(T0.UtcDateTime), d.DailyTrend[0].Date);
        Assert.Equal(1, d.DailyTrend[0].ResolvedCount);
        Assert.Equal(2, d.DailyTrend[1].ResolvedCount);

        Assert.Equal("op-1", d.ByOperator.First().OperatorId);
        Assert.Equal(2, d.ByOperator.First().ResolvedCount);
    }

    [Fact]
    public async Task ExecuteAsync_ResolvedIssues_AreOrderedByResolutionDateDescending()
    {
        using (var seed = db.CreateContext())
        {
            var t = TestData.SeedTopology(seed);
            TestData.AddReport(seed, t.ToiletId, ReportStatus.Resolved, T0, null, T0.AddMinutes(10), "primero");
            TestData.AddReport(seed, t.SinkId, ReportStatus.Resolved, T0, null, T0.AddMinutes(50), "ultimo");
        }

        using var context = db.CreateContext();
        var d = await SuperAdminHandler(context).ExecuteAsync();

        Assert.Equal(new[] { "ultimo", "primero" }, d.ResolvedIssues.Select(i => i.IssueDescription));
    }

    [Fact]
    public async Task ExecuteAsync_FiltersByBuildingAssetTypeAndDateRange()
    {
        Guid buildingA;
        using (var seed = db.CreateContext())
        {
            var a = TestData.SeedTopology(seed, "A");
            var b = TestData.SeedTopology(seed, "B");
            buildingA = a.BuildingId;
            TestData.AddReport(seed, a.ToiletId, ReportStatus.Resolved, T0, null, T0.AddDays(1), "a-toilet-day1");
            TestData.AddReport(seed, a.SinkId, ReportStatus.Resolved, T0, null, T0.AddDays(5), "a-sink-day5");
            TestData.AddReport(seed, a.ToiletId, ReportStatus.Resolved, T0, null, T0.AddDays(10), "a-toilet-day10");
            TestData.AddReport(seed, b.ToiletId, ReportStatus.Resolved, T0, null, T0.AddDays(5), "b-toilet-day5");
        }

        using var context = db.CreateContext();
        var handler = SuperAdminHandler(context);

        var byBuilding = await handler.ExecuteAsync(buildingId: buildingA);
        Assert.Equal(3, byBuilding.TotalResolved);

        var byType = await handler.ExecuteAsync(buildingId: buildingA, assetType: AssetType.Toilet);
        Assert.Equal(2, byType.TotalResolved);

        var byRange = await handler.ExecuteAsync(fromDate: T0.AddDays(2), toDate: T0.AddDays(6));
        Assert.Equal(new[] { "a-sink-day5", "b-toilet-day5" }, byRange.ResolvedIssues.Select(i => i.IssueDescription).OrderBy(x => x));
    }

    [Fact]
    public async Task ExecuteAsync_Admin_OnlyAggregatesAssignedBuildings()
    {
        string adminId;
        using (var seed = db.CreateContext())
        {
            var a = TestData.SeedTopology(seed, "Mio");
            var b = TestData.SeedTopology(seed, "Ajeno");
            TestData.AddReport(seed, a.ToiletId, ReportStatus.Resolved, T0, null, T0.AddMinutes(5));
            TestData.AddReport(seed, b.ToiletId, ReportStatus.Resolved, T0, null, T0.AddMinutes(5));
            TestData.AddReport(seed, b.SinkId);
            adminId = TestData.AddUser(seed, "admin@x.com", a.BuildingId).Id;
        }

        using var context = db.CreateContext();
        var d = await new GetResolvedIssuesDashboardQueryHandler(context, TestData.AccessorFor(adminId, "Admin")).ExecuteAsync();

        Assert.Equal(1, d.TotalResolved);
        Assert.Equal(0, d.TotalActive);
        Assert.All(d.ResolvedIssues, i => Assert.Equal("Mio", i.BuildingName));
    }

    [Fact]
    public async Task ExecuteAsync_ClockSkew_NeverProducesNegativeDurations()
    {
        using (var seed = db.CreateContext())
        {
            var t = TestData.SeedTopology(seed);
            TestData.AddReport(seed, t.ToiletId, ReportStatus.Resolved, T0, T0.AddMinutes(-3), T0.AddMinutes(-1));
        }

        using var context = db.CreateContext();
        var item = Assert.Single((await SuperAdminHandler(context).ExecuteAsync()).ResolvedIssues);

        Assert.Equal(0, item.MttrMinutes);
        Assert.Equal(0, item.MttaMinutes);
    }
}
