using Coworking.Shared.Enums;
using Coworking.Shared.Models.DTOs.Telemetry;

namespace CoworkingAdminWeb.Services;

public class GridEditorState
{
    public int GridWidth { get; set; } = 15;
    public int GridHeight { get; set; } = 15;

    public AssetType? DraggedAssetType { get; set; }

    public List<RestroomAssetDto> PlacedAssets { get; set; } = new();

    public event Action? OnStateChanged;

    public void SetDimensions(int width, int height)
    {
        GridWidth = width;
        GridHeight = height;
        NotifyStateChanged();
    }

    public void StartDrag(AssetType assetType)
    {
        DraggedAssetType = assetType;
    }

    public void ClearDrag()
    {
        DraggedAssetType = null;
    }

    public bool TryPlaceAsset(short x, short y)
    {
        if (DraggedAssetType == null) return false;

        // Validar colisiones lógicas
        if (PlacedAssets.Any(a => a.PositionX == x && a.PositionY == y))
        {
            return false; // Ya hay un elemento aquí
        }

        var newAsset = new RestroomAssetDto
        {
            Id = Guid.NewGuid(),
            Type = DraggedAssetType.Value,
            PositionX = x,
            PositionY = y,
            HasActiveIssue = false
        };

        PlacedAssets.Add(newAsset);
        ClearDrag();
        NotifyStateChanged();
        return true;
    }

    public void RemoveAsset(Guid id)
    {
        var asset = PlacedAssets.FirstOrDefault(a => a.Id == id);
        if (asset.Id != Guid.Empty)
        {
            PlacedAssets.Remove(asset);
            NotifyStateChanged();
        }
    }

    public void ClearGrid()
    {
        PlacedAssets.Clear();
        NotifyStateChanged();
    }

    private void NotifyStateChanged() => OnStateChanged?.Invoke();
}
