using StageSmith.Core.Constants;
using StageSmith.Core.Models;
using System.Diagnostics;

namespace StageSmith.Editor.Controls;

public class TilePaletteControl : DoubleBufferedPanel
{
    private Bitmap? _tileset;

    public int SelectedTileIndex { get; private set; } = -1;

    public event Action<int>? TileSelected;

    private const int TileSpacing = 2;

    public TilePaletteControl()
    {
        DoubleBuffered = true;
        AutoScroll = true;
        ResizeRedraw = true;
    }

    public void SetTileset(Bitmap? tileset)
    {
        _tileset = tileset;

        if (_tileset == null)
        {
            AutoScrollMinSize = Size.Empty;
            Invalidate();
            return;
        }

        var srcSize = MapConstants.DefaultTileSize;   // 16: タイル枚数の計算用
        var dstSize = ViewerConstants.TileRenderSize; // 32: 実際の表示サイズ
        var cols = _tileset.Width / srcSize;
        var rows = _tileset.Height / srcSize;

        AutoScrollMinSize = new Size(
            cols * (dstSize + TileSpacing),
            rows * (dstSize + TileSpacing));

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

        // ピクセルアート向けに最近傍補間を使用する
        g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
        g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;

        g.Clear(this.BackColor);

        var srcSize = MapConstants.DefaultTileSize;   // 16: 画像の切り出しサイズ
        var dstSize = ViewerConstants.TileRenderSize; // 32: 画面上の描画サイズ
        var tilesPerRow = _tileset.Width / srcSize;
        var tilesPerCol = _tileset.Height / srcSize;
        var totalTiles = tilesPerRow * tilesPerCol;

        var offset = AutoScrollPosition;

        for (var i = 0; i < totalTiles; i++)
        {
            // --- 元画像の切り出し（16×16）---
            var sx = (i % tilesPerRow) * srcSize;
            var sy = (i / tilesPerRow) * srcSize;
            var srcRect = new Rectangle(sx, sy, srcSize, srcSize);

            // --- 描画位置（32×32 に拡大）---
            var dx = (i % tilesPerRow) * (dstSize + TileSpacing);
            var dy = (i / tilesPerRow) * (dstSize + TileSpacing);
            var dstRect = new Rectangle(
                dx + offset.X,
                dy + offset.Y,
                dstSize,
                dstSize
            );

            // --- 拡大描画 ---
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

        var srcSize = MapConstants.DefaultTileSize;   // 16: タイル枚数の計算用
        var dstSize = ViewerConstants.TileRenderSize; // 32: クリック位置の計算用
        var offset = AutoScrollPosition;

        var rawX = e.X - offset.X;
        var rawY = e.Y - offset.Y;

        if (rawX < 0 || rawY < 0) return;

        var cellSize = dstSize + TileSpacing;

        var col = rawX / cellSize;
        var row = rawY / cellSize;

        var tilesPerRow = _tileset.Width / srcSize;
        var tilesPerCol = _tileset.Height / srcSize;

        if (col < 0 || col >= tilesPerRow ||
            row < 0 || row >= tilesPerCol)
            return;

        // --- 余白クリック防止 ---
        var offsetX = rawX % cellSize;
        var offsetY = rawY % cellSize;

        if (offsetX >= dstSize || offsetY >= dstSize)
            return;

        var index = row * tilesPerRow + col;

        SelectedTileIndex = index;
        TileSelected?.Invoke(index);

        Invalidate();
    }
}
