using Coworking.Shared.Enums;
using Coworking.Shared.Models.DTOs.Telemetry;

namespace CoworkingAdminWeb.Services;

public class GridEditorState
{
    public int GridWidth { get; set; } = 15;
    public int GridHeight { get; set; } = 15;

    public AssetType? DraggedAssetType { get; set; }
    public Guid? DraggedAssetId { get; set; }

    public List<RestroomAssetDto> PlacedAssets { get; set; } = new();

    public event Action? OnStateChanged;

    public void SetDimensions(int width, int height)
    {
        GridWidth = width;
        GridHeight = height;
        NotifyStateChanged();
    }

    public void StartDrag(AssetType assetType, Guid? assetId = null)
    {
        DraggedAssetType = assetType;
        DraggedAssetId = assetId;
    }

    public void ClearDrag()
    {
        DraggedAssetType = null;
        DraggedAssetId = null;
    }

    public bool TryPlaceAsset(short x, short y)
    {
        if (DraggedAssetType == null) return false;

        // Validar colisiones lógicas (excluyendo el propio elemento si se está moviendo)
        if (PlacedAssets.Any(a => a.PositionX == x && a.PositionY == y && a.Id != DraggedAssetId))
        {
            return false; // Ya hay un elemento aquí
        }

        if (DraggedAssetId.HasValue)
        {
            // Mover elemento existente
            var index = PlacedAssets.FindIndex(a => a.Id == DraggedAssetId.Value);
            if (index >= 0)
            {
                var asset = PlacedAssets[index];
                asset = asset with { PositionX = x, PositionY = y };
                PlacedAssets[index] = asset;
            }
        }
        else
        {
            var newAsset = new RestroomAssetDto
            {
                Id = Guid.NewGuid(),
                Type = DraggedAssetType.Value,
                PositionX = x,
                PositionY = y,
                HasActiveIssue = false
            };

            PlacedAssets.Add(newAsset);
        }

        ClearDrag();
        NotifyStateChanged();
        return true;
    }

    public void RemoveAsset(Guid id)
    {
        var asset = PlacedAssets.FirstOrDefault(a => a.Id == id);
        if (asset != null)
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

    public void LoadState(int width, int height, List<RestroomAssetDto> assets)
    {
        GridWidth = width;
        GridHeight = height;
        PlacedAssets = assets;
        NotifyStateChanged();
    }

    private void NotifyStateChanged() => OnStateChanged?.Invoke();
}
