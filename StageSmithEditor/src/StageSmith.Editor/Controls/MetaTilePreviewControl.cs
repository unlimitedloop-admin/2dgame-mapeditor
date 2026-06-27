using StageSmith.Core.Constants;
using StageSmith.Core.Models;

namespace StageSmith.Editor.Controls;

public sealed class MetaTilePreviewControl : DoubleBufferedPanel
{
    private MetaTile? _metaTile;
    private Bitmap? _tileset;

    private const int MinPreviewScale = 1;
    private const int MaxPreviewScale = 8;
    private const int DefaultPreviewScale = 3;

    private int _previewScale = DefaultPreviewScale;

    public int PreviewScale
    {
        get => _previewScale;
        private set
        {
            var clamped = Math.Clamp(value, MinPreviewScale, MaxPreviewScale);

            if (_previewScale == clamped)
                return;

            _previewScale = clamped;
            UpdateContentSize();
            Invalidate();
            PreviewScaleChanged?.Invoke(_previewScale);
        }
    }

    public event Action<int>? PreviewScaleChanged;

    public MetaTilePreviewControl()
    {
        DoubleBuffered = true;
        ResizeRedraw = true;
        BackColor = Color.FromArgb(16, 16, 16);
        TabStop = true;

        UpdateContentSize();
    }

    public void SetMetaTile(MetaTile? metaTile)
    {
        _metaTile = metaTile;
        UpdateContentSize();
        Invalidate();
    }

    public void SetTileset(Bitmap? tileset)
    {
        _tileset = tileset;
        Invalidate();
    }

    public void ZoomIn()
    {
        PreviewScale++;
    }

    public void ZoomOut()
    {
        PreviewScale--;
    }

    public void ResetZoom()
    {
        PreviewScale = DefaultPreviewScale;
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        base.OnMouseEnter(e);
        Focus();
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        Focus();
    }

    protected override void OnMouseWheel(MouseEventArgs e)
    {
        if ((ModifierKeys & Keys.Control) == Keys.Control)
        {
            if (e.Delta > 0)
                ZoomIn();
            else if (e.Delta < 0)
                ZoomOut();

            return;
        }

        base.OnMouseWheel(e);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        var g = e.Graphics;
        g.Clear(BackColor);

        if (_metaTile == null || _tileset == null)
            return;

        g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
        g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;

        var srcSize = MapConstants.DefaultTileSize;
        var dstSize = srcSize * PreviewScale;
        var tilesPerRow = Math.Max(1, _tileset.Width / srcSize);

        for (var y = 0; y < _metaTile.Height; y++)
        {
            for (var x = 0; x < _metaTile.Width; x++)
            {
                var tileId = _metaTile.GetTile(x, y);

                if (tileId == MetaTile.EmptyTile)
                    continue;

                var sx = (tileId % tilesPerRow) * srcSize;
                var sy = (tileId / tilesPerRow) * srcSize;

                var srcRect = new Rectangle(sx, sy, srcSize, srcSize);
                var dstRect = new Rectangle(
                    x * dstSize,
                    y * dstSize,
                    dstSize,
                    dstSize
                );

                g.DrawImage(_tileset, dstRect, srcRect, GraphicsUnit.Pixel);
            }
        }
    }

    private void UpdateContentSize()
    {
        if (_metaTile == null)
        {
            Size = new Size(200, 120);
            return;
        }

        var tileSize = MapConstants.DefaultTileSize * PreviewScale;

        Size = new Size(
            Math.Max(1, _metaTile.Width * tileSize),
            Math.Max(1, _metaTile.Height * tileSize)
        );
    }
}
