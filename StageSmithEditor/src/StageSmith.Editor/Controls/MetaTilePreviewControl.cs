using StageSmith.Core.Constants;
using StageSmith.Core.Models;

namespace StageSmith.Editor.Controls;

public sealed class MetaTilePreviewControl : DoubleBufferedPanel
{
    private MetaTile? _metaTile;
    private Bitmap? _tileset;

    private const int PreviewScale = 3;

    public MetaTilePreviewControl()
    {
        DoubleBuffered = true;
        ResizeRedraw = true;
        BackColor = Color.FromArgb(16, 16, 16);
    }

    public void SetMetaTile(MetaTile? metaTile)
    {
        _metaTile = metaTile;
        Invalidate();
    }

    public void SetTileset(Bitmap? tileset)
    {
        _tileset = tileset;
        Invalidate();
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
        var tilesPerRow = _tileset.Width / srcSize;

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
}
