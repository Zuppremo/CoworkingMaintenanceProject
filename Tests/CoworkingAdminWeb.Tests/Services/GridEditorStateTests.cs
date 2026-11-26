using Coworking.Shared.Enums;
using Coworking.Shared.Models.DTOs.Telemetry;
using CoworkingAdminWeb.Services;

namespace CoworkingAdminWeb.Tests.Services;

public class GridEditorStateTests
{
    [Fact]
    public void SetDimensions_UpdatesWidthHeightAndNotifiesListeners()
    {
        var state = new GridEditorState();
        var notified = 0;
        state.OnStateChanged += () => notified++;

        state.SetDimensions(20, 12);

        Assert.Equal(20, state.GridWidth);
        Assert.Equal(12, state.GridHeight);
        Assert.Equal(1, notified);
    }

    [Fact]
    public void TryPlaceAsset_WithoutActiveDrag_ReturnsFalse()
    {
        var state = new GridEditorState();

        Assert.False(state.TryPlaceAsset(2, 3));
        Assert.Empty(state.PlacedAssets);
    }

    [Fact]
    public void TryPlaceAsset_NewAssetOnEmptyCell_AddsAssetClearsDragAndNotifies()
    {
        var state = new GridEditorState();
        var notified = 0;
        state.OnStateChanged += () => notified++;
        state.StartDrag(AssetType.Toilet);

        var placed = state.TryPlaceAsset(4, 5);

        Assert.True(placed);
        var asset = Assert.Single(state.PlacedAssets);
        Assert.Equal(AssetType.Toilet, asset.Type);
        Assert.Equal((short)4, asset.PositionX);
        Assert.Equal((short)5, asset.PositionY);
        Assert.Null(state.DraggedAssetType);
        Assert.Null(state.DraggedAssetId);
        Assert.Equal(1, notified);
    }

    [Fact]
    public void TryPlaceAsset_CellOccupiedByAnotherAsset_ReturnsFalse()
    {
        var state = new GridEditorState();
        state.StartDrag(AssetType.Toilet);
        state.TryPlaceAsset(1, 1);

        state.StartDrag(AssetType.Sink);
        var placed = state.TryPlaceAsset(1, 1);

        Assert.False(placed);
        Assert.Single(state.PlacedAssets);
    }

    [Fact]
    public void TryPlaceAsset_FloorAndWallAssetOnSameCell_ShouldBeAllowedJustLikeBackend()
    {
        // SyncRestroomTopologyCommandHandler allows a Floor asset (Sink) and a Wall asset
        // (HandDryer) at the same (X, Y) coordinates, but GridEditorState blocks any overlap.
        var state = new GridEditorState();
        state.StartDrag(AssetType.Sink);
        Assert.True(state.TryPlaceAsset(2, 2));

        state.StartDrag(AssetType.HandDryer);
        var placed = state.TryPlaceAsset(2, 2);

        Assert.True(placed);
        Assert.Equal(2, state.PlacedAssets.Count);
    }

    [Fact]
    public void TryPlaceAsset_MovingExistingAsset_UpdatesCoordinatesWithoutDuplicating()
    {
        var state = new GridEditorState();
        state.StartDrag(AssetType.PaperDispenser);
        state.TryPlaceAsset(1, 1);
        var id = state.PlacedAssets.Single().Id;

        state.StartDrag(AssetType.PaperDispenser, id);
        var moved = state.TryPlaceAsset(6, 7);

        Assert.True(moved);
        var asset = Assert.Single(state.PlacedAssets);
        Assert.Equal(id, asset.Id);
        Assert.Equal(((short)6, (short)7), (asset.PositionX, asset.PositionY));
    }

    [Fact]
    public void RemoveAsset_ExistingId_RemovesAndNotifies()
    {
        var state = new GridEditorState();
        state.StartDrag(AssetType.Toilet);
        state.TryPlaceAsset(0, 0);
        var id = state.PlacedAssets.Single().Id;
        var notified = 0;
        state.OnStateChanged += () => notified++;

        state.RemoveAsset(id);

        Assert.Empty(state.PlacedAssets);
        Assert.Equal(1, notified);
    }

    [Fact]
    public void RemoveAsset_UnknownId_DoesNotNotify()
    {
        var state = new GridEditorState();
        var notified = 0;
        state.OnStateChanged += () => notified++;

        state.RemoveAsset(Guid.NewGuid());

        Assert.Equal(0, notified);
    }

    [Fact]
    public void LoadState_AndClearGrid_UpdateStateAndNotify()
    {
        var state = new GridEditorState();
        var assets = new List<RestroomAssetDto>
        {
            new() { Id = Guid.NewGuid(), Type = AssetType.Sink, PositionX = 3, PositionY = 4 }
        };

        state.LoadState(8, 9, assets);
        Assert.Equal(8, state.GridWidth);
        Assert.Equal(9, state.GridHeight);
        Assert.Single(state.PlacedAssets);

        state.ClearGrid();
        Assert.Empty(state.PlacedAssets);
    }
}
