using Coworking.Shared.Enums;
using Coworking.Shared.Models.DTOs;
using Coworking.Shared.Models.DTOs.Telemetry;
using Coworking.Shared.Models.Interfaces;
using CoworkingAPI.Features.IssueReports;
using CoworkingAPI.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace CoworkingAPI.Tests.Features.IssueReports;

public class CreateIssueReportCommandHandlerTests : IDisposable
{
    private readonly SqliteTestDatabase db = new();
    private readonly Mock<IAlertPublisher> publisher = new();

    public void Dispose() => db.Dispose();

    private CreateIssueReportCommandHandler CreateHandler(out CoworkingAPI.Data.AppDbContext context)
    {
        context = db.CreateContext();
        return new CreateIssueReportCommandHandler(context, publisher.Object);
    }

    [Fact]
    public async Task ExecuteAsync_ValidRequest_PersistsReportWithGeneratedStatus()
    {
        SeededTopology topology;
        using (var seed = db.CreateContext()) topology = TestData.SeedTopology(seed);

        var handler = CreateHandler(out var context);
        var result = await handler.ExecuteAsync(new CreateIssueReportRequest
        {
            RestroomAssetId = topology.ToiletId,
            IssueDescription = "Inodoro tapado",
            ReportingUserId = "coworker-42"
        });

        Assert.True(result.Success);
        Assert.Equal(string.Empty, result.Error);

        using var verify = db.CreateContext();
        var report = await verify.IssueReports.SingleAsync();
        Assert.Equal(topology.ToiletId, report.RestroomAssetId);
        Assert.Equal("Inodoro tapado", report.IssueDescription);
        Assert.Equal("coworker-42", report.ReportingUserId);
        Assert.Equal(ReportStatus.Generated, report.Status);
        Assert.Null(report.AttentionDate);
        Assert.Null(report.ResolutionDate);
    }

    [Fact]
    public async Task ExecuteAsync_ValidRequest_BroadcastsToTheAssetBuildingGroup()
    {
        SeededTopology topology;
        using (var seed = db.CreateContext()) topology = TestData.SeedTopology(seed);

        var handler = CreateHandler(out _);
        await handler.ExecuteAsync(new CreateIssueReportRequest
        {
            RestroomAssetId = topology.ToiletId,
            IssueDescription = "Sin papel",
            ReportingUserId = "x"
        });

        publisher.Verify(p => p.BroadcastStatusUpdateAsync(
            It.Is<ReportStatusUpdateTelemetry>(t => t.Status == ReportStatus.Generated && t.ReportId != Guid.Empty),
            topology.BuildingId), Times.Once);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ExecuteAsync_BlankReportingUser_FallsBackToAnonymousCoworker(string reportingUser)
    {
        SeededTopology topology;
        using (var seed = db.CreateContext()) topology = TestData.SeedTopology(seed);

        var handler = CreateHandler(out _);
        await handler.ExecuteAsync(new CreateIssueReportRequest
        {
            RestroomAssetId = topology.ToiletId,
            IssueDescription = "Sin papel",
            ReportingUserId = reportingUser
        });

        using var verify = db.CreateContext();
        Assert.Equal("anonymous-coworker", (await verify.IssueReports.SingleAsync()).ReportingUserId);
    }

    [Theory]
    [InlineData(ReportStatus.Generated)]
    [InlineData(ReportStatus.InProgress)]
    public async Task ExecuteAsync_AssetAlreadyHasActiveReport_ReturnsConflictAndDoesNotBroadcast(ReportStatus activeStatus)
    {
        SeededTopology topology;
        using (var seed = db.CreateContext())
        {
            topology = TestData.SeedTopology(seed);
            TestData.AddReport(seed, topology.ToiletId, activeStatus);
        }

        var handler = CreateHandler(out _);
        var result = await handler.ExecuteAsync(new CreateIssueReportRequest
        {
            RestroomAssetId = topology.ToiletId,
            IssueDescription = "Otra vez",
            ReportingUserId = "x"
        });

        Assert.False(result.Success);
        Assert.False(string.IsNullOrWhiteSpace(result.Error));
        using var verify = db.CreateContext();
        Assert.Equal(1, await verify.IssueReports.CountAsync());
        publisher.Verify(p => p.BroadcastStatusUpdateAsync(It.IsAny<ReportStatusUpdateTelemetry>(), It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_PreviousReportResolved_AllowsANewReport()
    {
        SeededTopology topology;
        using (var seed = db.CreateContext())
        {
            topology = TestData.SeedTopology(seed);
            TestData.AddReport(seed, topology.ToiletId, ReportStatus.Resolved,
                resolutionDate: TestData.BaseTime.AddMinutes(10));
        }

        var handler = CreateHandler(out _);
        var result = await handler.ExecuteAsync(new CreateIssueReportRequest
        {
            RestroomAssetId = topology.ToiletId,
            IssueDescription = "Nuevo problema",
            ReportingUserId = "x"
        });

        Assert.True(result.Success);
        using var verify = db.CreateContext();
        Assert.Equal(2, await verify.IssueReports.CountAsync());
    }

    [Fact]
    public async Task ExecuteAsync_ActiveReportOnDifferentAsset_DoesNotBlock()
    {
        SeededTopology topology;
        using (var seed = db.CreateContext())
        {
            topology = TestData.SeedTopology(seed);
            TestData.AddReport(seed, topology.SinkId);
        }

        var handler = CreateHandler(out _);
        var result = await handler.ExecuteAsync(new CreateIssueReportRequest
        {
            RestroomAssetId = topology.ToiletId,
            IssueDescription = "Inodoro",
            ReportingUserId = "x"
        });

        Assert.True(result.Success);
    }
}

public class AttendIssueReportCommandHandlerTests : IDisposable
{
    private readonly SqliteTestDatabase db = new();
    private readonly Mock<IAlertPublisher> publisher = new();

    public void Dispose() => db.Dispose();

    [Fact]
    public async Task ExecuteAsync_UnknownReport_ReturnsFalse()
    {
        using var context = db.CreateContext();
        var handler = new AttendIssueReportCommandHandler(context, publisher.Object);

        Assert.False(await handler.ExecuteAsync(Guid.NewGuid(), "op"));
        publisher.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ExecuteAsync_GeneratedReport_MarksInProgressAssignsOperatorAndBroadcasts()
    {
        SeededTopology topology;
        Guid reportId;
        using (var seed = db.CreateContext())
        {
            topology = TestData.SeedTopology(seed);
            reportId = TestData.AddReport(seed, topology.ToiletId).Id;
        }

        using (var context = db.CreateContext())
        {
            var handler = new AttendIssueReportCommandHandler(context, publisher.Object);
            Assert.True(await handler.ExecuteAsync(reportId, "operario-1"));
        }

        using var verify = db.CreateContext();
        var report = await verify.IssueReports.SingleAsync(r => r.Id == reportId);
        Assert.Equal(ReportStatus.InProgress, report.Status);
        Assert.NotNull(report.AttentionDate);
        Assert.Equal("operario-1", report.AssignedOperatorId);

        publisher.Verify(p => p.BroadcastStatusUpdateAsync(
            It.Is<ReportStatusUpdateTelemetry>(t => t.ReportId == reportId && t.Status == ReportStatus.InProgress),
            topology.BuildingId), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_BlankOperator_KeepsPreviouslyAssignedOperator()
    {
        Guid reportId;
        using (var seed = db.CreateContext())
        {
            var topology = TestData.SeedTopology(seed);
            reportId = TestData.AddReport(seed, topology.ToiletId, assignedOperatorId: "previo").Id;
        }

        using (var context = db.CreateContext())
        {
            await new AttendIssueReportCommandHandler(context, publisher.Object).ExecuteAsync(reportId, "  ");
        }

        using var verify = db.CreateContext();
        Assert.Equal("previo", (await verify.IssueReports.SingleAsync()).AssignedOperatorId);
    }

    [Fact]
    public async Task ExecuteAsync_ReportAlreadyInProgress_DoesNotOverwriteOriginalAttentionDate()
    {
        // MTTA metrics depend on the first attention timestamp. The legacy IssuesController
        // explicitly guarded this (AttentionDate == null), the new handler should too.
        var originalAttention = TestData.BaseTime.AddMinutes(5);
        Guid reportId;
        using (var seed = db.CreateContext())
        {
            var topology = TestData.SeedTopology(seed);
            reportId = TestData.AddReport(seed, topology.ToiletId, ReportStatus.InProgress,
                attentionDate: originalAttention).Id;
        }

        using (var context = db.CreateContext())
        {
            await new AttendIssueReportCommandHandler(context, publisher.Object).ExecuteAsync(reportId, "op-2");
        }

        using var verify = db.CreateContext();
        Assert.Equal(originalAttention, (await verify.IssueReports.SingleAsync()).AttentionDate);
    }

    [Fact]
    public async Task ExecuteAsync_ResolvedReport_IsNotReopened()
    {
        // Attending an already resolved report must not move it back to InProgress
        // (it would reappear in the cleaning staff list and corrupt the dashboard metrics).
        Guid reportId;
        using (var seed = db.CreateContext())
        {
            var topology = TestData.SeedTopology(seed);
            reportId = TestData.AddReport(seed, topology.ToiletId, ReportStatus.Resolved,
                attentionDate: TestData.BaseTime.AddMinutes(2),
                resolutionDate: TestData.BaseTime.AddMinutes(10)).Id;
        }

        bool result;
        using (var context = db.CreateContext())
        {
            result = await new AttendIssueReportCommandHandler(context, publisher.Object).ExecuteAsync(reportId, "op");
        }

        using var verify = db.CreateContext();
        Assert.Equal(ReportStatus.Resolved, (await verify.IssueReports.SingleAsync()).Status);
        Assert.False(result);
    }
}

public class ResolveIssueReportCommandHandlerTests : IDisposable
{
    private readonly SqliteTestDatabase db = new();
    private readonly Mock<IAlertPublisher> publisher = new();

    public void Dispose() => db.Dispose();

    [Fact]
    public async Task ExecuteAsync_UnknownReport_ReturnsFalse()
    {
        using var context = db.CreateContext();
        Assert.False(await new ResolveIssueReportCommandHandler(context, publisher.Object).ExecuteAsync(Guid.NewGuid()));
        publisher.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(ReportStatus.Generated)]
    [InlineData(ReportStatus.InProgress)]
    public async Task ExecuteAsync_ActiveReport_MarksResolvedAndBroadcasts(ReportStatus initialStatus)
    {
        SeededTopology topology;
        Guid reportId;
        using (var seed = db.CreateContext())
        {
            topology = TestData.SeedTopology(seed);
            reportId = TestData.AddReport(seed, topology.SinkId, initialStatus).Id;
        }

        using (var context = db.CreateContext())
        {
            Assert.True(await new ResolveIssueReportCommandHandler(context, publisher.Object).ExecuteAsync(reportId, "op-9"));
        }

        using var verify = db.CreateContext();
        var report = await verify.IssueReports.SingleAsync();
        Assert.Equal(ReportStatus.Resolved, report.Status);
        Assert.NotNull(report.ResolutionDate);
        Assert.Equal("op-9", report.AssignedOperatorId);

        publisher.Verify(p => p.BroadcastStatusUpdateAsync(
            It.Is<ReportStatusUpdateTelemetry>(t => t.ReportId == reportId && t.Status == ReportStatus.Resolved),
            topology.BuildingId), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_AfterResolving_AssetAcceptsNewReports()
    {
        SeededTopology topology;
        Guid reportId;
        using (var seed = db.CreateContext())
        {
            topology = TestData.SeedTopology(seed);
            reportId = TestData.AddReport(seed, topology.ToiletId).Id;
        }

        using (var context = db.CreateContext())
        {
            await new ResolveIssueReportCommandHandler(context, publisher.Object).ExecuteAsync(reportId);
        }

        using var createContext = db.CreateContext();
        var result = await new CreateIssueReportCommandHandler(createContext, publisher.Object)
            .ExecuteAsync(new CreateIssueReportRequest
            {
                RestroomAssetId = topology.ToiletId,
                IssueDescription = "Nuevo",
                ReportingUserId = "x"
            });
        Assert.True(result.Success);
    }

    [Fact]
    public async Task ExecuteAsync_AlreadyResolvedReport_KeepsOriginalResolutionDate()
    {
        // Resolving twice (e.g. two operators tapping "Resolver" at the same time) must not
        // rewrite the resolution timestamp, otherwise MTTR is inflated.
        var originalResolution = TestData.BaseTime.AddMinutes(30);
        Guid reportId;
        using (var seed = db.CreateContext())
        {
            var topology = TestData.SeedTopology(seed);
            reportId = TestData.AddReport(seed, topology.ToiletId, ReportStatus.Resolved,
                resolutionDate: originalResolution, assignedOperatorId: "op-original").Id;
        }

        using (var context = db.CreateContext())
        {
            await new ResolveIssueReportCommandHandler(context, publisher.Object).ExecuteAsync(reportId, "op-tarde");
        }

        using var verify = db.CreateContext();
        var report = await verify.IssueReports.SingleAsync();
        Assert.Equal(originalResolution, report.ResolutionDate);
        Assert.Equal("op-original", report.AssignedOperatorId);
    }
}
