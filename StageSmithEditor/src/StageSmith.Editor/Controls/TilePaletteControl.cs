using StageSmith.Core.Constants;

namespace StageSmith.Editor.Controls;

public class TilePaletteControl : DoubleBufferedPanel
{
    private Bitmap? _tileset;

    public int SelectedTileIndex { get; private set; } = -1;

    public event Action<int>? TileSelected;

    private const int _tileSpacing = 2;

    public TilePaletteControl()
    {
        DoubleBuffered = true;
        AutoScroll = true;
        ResizeRedraw = true;
    }

    public void SetTileset(Bitmap tileset)
    {
        _tileset = tileset;

        var tileSize = MapConstants.TilePixelSize;
        var cols = _tileset.Width / tileSize;
        var rows = _tileset.Height / tileSize;

        AutoScrollMinSize = new Size(
            cols * (tileSize + _tileSpacing),
            rows * (tileSize + _tileSpacing));

        Invalidate();
    }

    public void SetSelected(int index)
    {
        SelectedTileIndex = index;
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        if (_tileset == null) return;

        var g = e.Graphics;
        g.Clear(this.BackColor);

        var tileSize = MapConstants.TilePixelSize;
        var tilesPerRow = _tileset.Width / tileSize;
        var tilesPerCol = _tileset.Height / tileSize;
        var totalTiles = tilesPerRow * tilesPerCol;

        var offset = AutoScrollPosition;

        for (var i = 0; i < totalTiles; i++)
        {
            // --- 元画像の切り出し ---
            var sx = (i % tilesPerRow) * tileSize;
            var sy = (i / tilesPerRow) * tileSize;

            var srcRect = new Rectangle(sx, sy, tileSize, tileSize);

            // --- 描画位置 ---
            var dx = (i % tilesPerRow) * (tileSize + _tileSpacing);
            var dy = (i / tilesPerRow) * (tileSize + _tileSpacing);

            var dstRect = new Rectangle(
                dx + offset.X,
                dy + offset.Y,
                tileSize,
                tileSize
            );

            // --- 描画 ---
            g.DrawImage(_tileset, dstRect, srcRect, GraphicsUnit.Pixel);

            // --- 選択枠 ---
            if (i == SelectedTileIndex)
            {
                var rect = new Rectangle(
                    dstRect.X,
                    dstRect.Y,
                    dstRect.Width - 1,
                    dstRect.Height - 1
                );

                using var pen = new Pen(Color.FromArgb(180, Color.Red), 2);
                g.DrawRectangle(pen, rect);
            }
        }
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);

        if (_tileset == null) return;

        var tileSize = MapConstants.TilePixelSize;
        var offset = AutoScrollPosition;

        var rawX = e.X - offset.X;
        var rawY = e.Y - offset.Y;

        if (rawX < 0 || rawY < 0) return;

        var cellSize = tileSize + _tileSpacing;

        var col = rawX / cellSize;
        var row = rawY / cellSize;

        var tilesPerRow = _tileset.Width / tileSize;
        var tilesPerCol = _tileset.Height / tileSize;

        if (col < 0 || col >= tilesPerRow ||
            row < 0 || row >= tilesPerCol)
            return;

        // --- 余白クリック防止 ---
        var offsetX = rawX % cellSize;
        var offsetY = rawY % cellSize;

        if (offsetX >= tileSize || offsetY >= tileSize)
            return;

        var index = row * tilesPerRow + col;

        SelectedTileIndex = index;
        TileSelected?.Invoke(index);

        Invalidate();
    }
}
