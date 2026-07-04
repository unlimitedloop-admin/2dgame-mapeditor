using StageSmith.Core.Constants;

namespace StageSmith.Editor.Controls;

public class TilePaletteControl : DoubleBufferedPanel
{
    private Bitmap? _tileset;
    private bool _ownsTileset;

    public int SelectedTileIndex { get; private set; } = -1;

    public event Action<int>? TileSelected;
    public event EventHandler? TilesetImageSelectionRequested;

    private const int TileSpacing = 2;

    private readonly ContextMenuStrip _contextMenu;
    private readonly ToolStripMenuItem _selectTilesetImageMenuItem;

    public TilePaletteControl()
    {
        DoubleBuffered = true;
        AutoScroll = true;
        ResizeRedraw = true;
        TabStop = true;

        _selectTilesetImageMenuItem = new ToolStripMenuItem("タイルセット画像を選択...");
        _selectTilesetImageMenuItem.Click += (_, _) =>
        {
            TilesetImageSelectionRequested?.Invoke(this, EventArgs.Empty);
        };

        _contextMenu = new ContextMenuStrip();
        _contextMenu.Items.Add(_selectTilesetImageMenuItem);

        ContextMenuStrip = _contextMenu;
    }

    public void SetTileset(Bitmap? tileset)
    {
        ReplaceTileset(tileset);

        var usableTileset = GetUsableTileset();

        if (usableTileset == null)
        {
            SelectedTileIndex = -1;
        }
        else
        {
            var tileCount =
                (usableTileset.Width / MapConstants.DefaultTileSize) *
                (usableTileset.Height / MapConstants.DefaultTileSize);

            if (SelectedTileIndex >= tileCount)
                SelectedTileIndex = -1;
        }

        UpdateScrollSize();
        Invalidate();
    }

    public void SetSelected(int index)
    {
        SelectedTileIndex = index;
        Invalidate();
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

        // 右クリックは ContextMenuStrip に任せる。
        // タイル選択処理には入らない。
        if (e.Button == MouseButtons.Right)
            return;

        // 左クリック以外ではタイル選択しない。
        if (e.Button != MouseButtons.Left)
            return;

        var tileset = GetUsableTileset();
        if (tileset == null) return;

        var srcSize = MapConstants.DefaultTileSize;   // 16: タイル枚数の計算用
        var dstSize = ViewerConstants.TileRenderSize; // 32: クリック位置の計算用
        var offset = AutoScrollPosition;

        var rawX = e.X - offset.X;
        var rawY = e.Y - offset.Y;

        if (rawX < 0 || rawY < 0) return;

        var cellSize = dstSize + TileSpacing;

        var col = rawX / cellSize;
        var row = rawY / cellSize;

        var tilesPerRow = tileset.Width / srcSize;
        var tilesPerCol = tileset.Height / srcSize;

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

    protected override void OnMouseWheel(MouseEventArgs e)
    {
        if ((ModifierKeys & Keys.Shift) == Keys.Shift)
        {
            ScrollHorizontal(e.Delta);
            return;
        }

        base.OnMouseWheel(e);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        var tileset = GetUsableTileset();
        if (tileset == null) return;

        var g = e.Graphics;

        // ピクセルアート向けに最近傍補間を使用する
        g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
        g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;

        g.Clear(BackColor);

        var srcSize = MapConstants.DefaultTileSize;   // 16: 画像の切り出しサイズ
        var dstSize = ViewerConstants.TileRenderSize; // 32: 画面上の描画サイズ
        var tilesPerRow = tileset.Width / srcSize;
        var tilesPerCol = tileset.Height / srcSize;
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
            g.DrawImage(tileset, dstRect, srcRect, GraphicsUnit.Pixel);

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

    private void ScrollHorizontal(int wheelDelta)
    {
        if (!HorizontalScroll.Visible)
            return;

        var step = Math.Max(32, ViewerConstants.TileRenderSize + TileSpacing);
        var direction = wheelDelta > 0 ? -1 : 1;

        var requestedValue = HorizontalScroll.Value + (direction * step);
        var maxValue = Math.Max(
            HorizontalScroll.Minimum,
            HorizontalScroll.Maximum - HorizontalScroll.LargeChange + 1
        );

        var newValue = Math.Clamp(
            requestedValue,
            HorizontalScroll.Minimum,
            maxValue
        );

        if (HorizontalScroll.Value == newValue)
            return;

        HorizontalScroll.Value = newValue;
        AutoScrollPosition = new Point(newValue, -AutoScrollPosition.Y);
        Invalidate();
    }

    private void UpdateScrollSize()
    {
        var tileset = GetUsableTileset();

        if (tileset == null)
        {
            AutoScrollMinSize = Size.Empty;
            return;
        }

        var srcSize = MapConstants.DefaultTileSize;   // 16: タイル枚数の計算用
        var dstSize = ViewerConstants.TileRenderSize; // 32: 実際の表示サイズ
        var cols = tileset.Width / srcSize;
        var rows = tileset.Height / srcSize;

        AutoScrollMinSize = new Size(
            cols * (dstSize + TileSpacing),
            rows * (dstSize + TileSpacing));
    }

    private void ReplaceTileset(Bitmap? source)
    {
        DisposeOwnedTileset();

        if (source == null)
            return;

        try
        {
            _tileset = new Bitmap(source);
            _ownsTileset = true;
        }
        catch (ArgumentException)
        {
            _tileset = null;
            _ownsTileset = false;
        }
        catch (ObjectDisposedException)
        {
            _tileset = null;
            _ownsTileset = false;
        }
    }

    private Bitmap? GetUsableTileset()
    {
        var tileset = _tileset;

        if (tileset == null)
            return null;

        return IsBitmapUsable(tileset) ? tileset : null;
    }

    private static bool IsBitmapUsable(Bitmap bitmap)
    {
        try
        {
            _ = bitmap.Width;
            _ = bitmap.Height;
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
        catch (ObjectDisposedException)
        {
            return false;
        }
    }

    private void DisposeOwnedTileset()
    {
        if (_ownsTileset)
        {
            _tileset?.Dispose();
        }

        _tileset = null;
        _ownsTileset = false;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            DisposeOwnedTileset();

            _selectTilesetImageMenuItem.Dispose();
            _contextMenu.Dispose();
        }

        base.Dispose(disposing);
    }
}
