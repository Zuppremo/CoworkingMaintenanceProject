using Coworking.Shared.Enums;
using Coworking.Shared.Extensions;
using Coworking.Shared.Models;
using Coworking.Shared.Models.DTOs.Telemetry;
using MessagePack;

namespace Coworking.Shared.Tests;

public class AssetTypeExtensionsTests
{
    [Theory]
    [InlineData(AssetType.Toilet, MountingLayer.Floor)]
    [InlineData(AssetType.Sink, MountingLayer.Floor)]
    [InlineData(AssetType.HandDryer, MountingLayer.Wall)]
    [InlineData(AssetType.PaperDispenser, MountingLayer.Wall)]
    public void GetMountingLayer_ReturnsExpectedLayer(AssetType assetType, MountingLayer expected)
    {
        Assert.Equal(expected, assetType.GetMountingLayer());
    }

    [Fact]
    public void GetMountingLayer_UnknownEnumValue_FallsBackToFloor()
    {
        var unknown = (AssetType)99;
        Assert.Equal(MountingLayer.Floor, unknown.GetMountingLayer());
    }
}

public class DomainModelsTests
{
    [Fact]
    public void AssetSubtypes_InitializeWithMatchingAssetType()
    {
        Assert.Equal(AssetType.Toilet, new Toilet().Type);
        Assert.Equal(AssetType.Sink, new Sink().Type);
        Assert.Equal(AssetType.HandDryer, new HandDryer().Type);
        Assert.Equal(AssetType.PaperDispenser, new PaperDispenser().Type);
    }

    [Fact]
    public void IssueReport_DefaultsToGeneratedStatusAndNotDeleted()
    {
        var report = new IssueReport();

        Assert.Equal(ReportStatus.Generated, report.Status);
        Assert.False(report.IsDeleted);
        Assert.Null(report.AttentionDate);
        Assert.Null(report.ResolutionDate);
        Assert.Equal(string.Empty, report.ReportingUserId);
        Assert.Equal(string.Empty, report.AssignedOperatorId);
    }
}

public class TelemetrySerializationTests
{
    [Fact]
    public void ReportStatusUpdateTelemetry_RoundTripsWithMessagePack()
    {
        var original = new ReportStatusUpdateTelemetry
        {
            ReportId = Guid.NewGuid(),
            Status = ReportStatus.InProgress
        };

        var bytes = MessagePackSerializer.Serialize(original);
        var deserialized = MessagePackSerializer.Deserialize<ReportStatusUpdateTelemetry>(bytes);

        Assert.Equal(original, deserialized);
    }

    [Fact]
    public void RestroomAssetDto_RoundTripsWithMessagePack()
    {
        var original = new RestroomAssetDto
        {
            Id = Guid.NewGuid(),
            Type = AssetType.PaperDispenser,
            PositionX = 4,
            PositionY = 7,
            HasActiveIssue = true
        };

        var bytes = MessagePackSerializer.Serialize(original);
        var deserialized = MessagePackSerializer.Deserialize<RestroomAssetDto>(bytes);

        Assert.Equal(original, deserialized);
    }
}
