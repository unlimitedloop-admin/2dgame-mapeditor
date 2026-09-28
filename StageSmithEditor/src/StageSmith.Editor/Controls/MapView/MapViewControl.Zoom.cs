using StageSmith.Core.Constants;
using System.ComponentModel;

namespace StageSmith.Editor.Controls;

// MapViewControl のズーム専用partial。
// 分割方針は MapViewControl.cs 参照。
public partial class MapViewControl
{
    // ===== Zoom =====
    private const float MinZoomScale = 0.5f;
    private const float DefaultZoomScale = 1.0f;
    private const float MaxZoomScale = 2.0f;
    private const float ZoomStep = 0.5f;

    private float _zoomScale = DefaultZoomScale;

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public float ZoomScale => _zoomScale;

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int CurrentTileRenderSize
        => Math.Max(1, (int)MathF.Round(ViewerConstants.TileRenderSize * _zoomScale));

    /// <summary>
    /// 部屋内ピクセル1pxあたりの画面ピクセル数（エンティティ配置の座標変換用）。
    /// </summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public float RoomPixelScale => CurrentTileRenderSize / (float)MapConstants.DefaultTileSize;

    public bool CanZoomIn => _zoomScale < MaxZoomScale;
    public bool CanZoomOut => _zoomScale > MinZoomScale;
    public bool IsDefaultZoom => Math.Abs(_zoomScale - DefaultZoomScale) < 0.001f;

    public event EventHandler? ZoomChanged;

    public void ZoomIn()
    {
        SetZoomScale(_zoomScale + ZoomStep);
    }

    public void ZoomOut()
    {
        SetZoomScale(_zoomScale - ZoomStep);
    }

    public void ResetZoom()
    {
        SetZoomScale(DefaultZoomScale);
    }

    public void SetZoomScale(float scale)
    {
        var clamped = Math.Clamp(scale, MinZoomScale, MaxZoomScale);

        if (Math.Abs(_zoomScale - clamped) < 0.001f)
            return;

        _zoomScale = clamped;

        UpdatePreferredControlSize();

        ZoomChanged?.Invoke(this, EventArgs.Empty);
        Invalidate();
    }

    public Size GetPreferredContentSize()
    {
        var width = (_tileMap?.Width ?? MapConstants.PageTileWidth) * CurrentTileRenderSize;
        var height = (_tileMap?.Height ?? MapConstants.PageTileHeight) * CurrentTileRenderSize;

        return new Size(
            width + OffsetX + ViewerConstants.MapViewMargin,
            height + OffsetY + ViewerConstants.MapViewMargin
        );
    }

    private void UpdatePreferredControlSize()
    {
        var size = GetPreferredContentSize();

        MinimumSize = size;

        if (Dock == DockStyle.None)
        {
            Size = size;
        }
    }
}
