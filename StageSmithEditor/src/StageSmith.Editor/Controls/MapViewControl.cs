using System.ComponentModel;
using StageSmith.Core.Constants;
using StageSmith.Core.Models;
using StageSmith.Editor.Tools;

namespace StageSmith.Editor.Controls;

public class MapViewControl : DoubleBufferedPanel
{
    private TileMap? _tileMap;
    private Bitmap? _tileset;
    private bool _ownsTileset;

    private bool _showGrid = true;

    private Point _hoverTile = new(-1, -1);

    // ===== Tool =====
    private ToolManager? _toolManager;

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public ToolManager? ToolManager
    {
        get => _toolManager;
        set
        {
            _toolManager?.ToolChanged -= OnToolChanged;
            _toolManager = value;
            _toolManager?.ToolChanged += OnToolChanged;
            Invalidate();
        }
    }

    private void OnToolChanged(ITool? tool)
    {
        Cursor = Cursors.Default;
        Invalidate();
    }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public ITool? PickerTool { get; set; }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public ITool? FillTool { get; set; }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public SelectionTool? SelectionTool { get; set; }

    // ===== Preview =====
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int PreviewTileId { get; set; } = -1;

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public MetaTile? PreviewMetaTile { get; set; }

    /// <summary>
    /// 外部側で特殊ブラシを処理したい場合、MapViewControl内部のTool処理を抑止する。
    /// 例：MetaTile配置中はPenTool/SelectionToolへ入力を渡さない。
    /// </summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Func<bool>? ShouldSuppressToolInput { get; set; }

    // ===== Paste Preview =====
    private bool _showPreview = false;
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool ShowPreview
    {
        get => _showPreview;
        set
        {
            if (_showPreview == value) return;
            _showPreview = value;
            Invalidate();
        }
    }

    // ===== Adjacent Navigation =====
    public event EventHandler<AdjacentNavigationRequestedEventArgs>? AdjacentNavigationRequested;

    private MapAdjacentState _adjacentState = new();

    public void SetAdjacentState(MapAdjacentState state)
    {
        _adjacentState = state;
        Invalidate();
    }

    public MapViewControl()
    {
        DoubleBuffered = true;
        ResizeRedraw = true;
    }

    // =========================
    // セット系
    // =========================
    public void SetTileMap(TileMap? map)
    {
        _tileMap = map;
        Invalidate();
    }

    public void SetTileset(Bitmap? tileset)
    {
        ReplaceTileset(tileset);
        Invalidate();
    }

    public void SetShowGrid(bool show)
    {
        _showGrid = show;
        Invalidate();
    }

    public Point GetHoverTile() => _hoverTile;

    // =========================
    // 描画
    // =========================
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        if (_tileMap == null) return;

        var g = e.Graphics;

        // ピクセルアート向けに最近傍補間を使用する。
        // デフォルトのバイリニア補間だと拡大時に隣接タイルが滲んで混入するため。
        g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
        g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;

        g.Clear(BackColor);

        if (GetUsableTileset() != null)
        {
            DrawTiles(g);
        }

        if (_showGrid)
        {
            DrawGrid(g);
        }

        DrawPreview(g);

        // SelectionToolに描かせる
        SelectionTool?.DrawOverlay(g, ViewerConstants.TileRenderSize, ViewerConstants.MapViewMargin);
        SelectionTool?.DrawMovingOverlay(g, ViewerConstants.TileRenderSize, GetUsableTileset(), ViewerConstants.MapViewMargin);

        DrawAdjacentNavigationButtons(g);

        // Extended add plus cursor at copy mode
        if (SelectionTool?.IsCopyModeActive() == true)
        {
            DrawPlusCursor(g);
        }
    }

    private void DrawTiles(Graphics g)
    {
        var tileset = GetUsableTileset();
        if (_tileMap == null || tileset == null) return;

        var srcSize = MapConstants.DefaultTileSize;   // 16: 画像の切り出しサイズ
        var dstSize = ViewerConstants.TileRenderSize; // 32: 画面上の描画サイズ
        var margin  = ViewerConstants.MapViewMargin;  // 32: 上下左右マージン
        var tilesPerRow = tileset.Width / srcSize;

        for (var y = 0; y < _tileMap.Height; y++)
        {
            for (var x = 0; x < _tileMap.Width; x++)
            {
                var tileId = _tileMap.GetTile(x, y);

                var sx = (tileId % tilesPerRow) * srcSize;
                var sy = (tileId / tilesPerRow) * srcSize;

                var srcRect = new Rectangle(sx, sy, srcSize, srcSize);
                var dstRect = new Rectangle(
                    margin + x * dstSize,
                    margin + y * dstSize,
                    dstSize,
                    dstSize);

                g.DrawImage(tileset, dstRect, srcRect, GraphicsUnit.Pixel);
            }
        }
    }

    private void DrawGrid(Graphics g)
    {
        if (_tileMap == null) return;

        var dstSize = ViewerConstants.TileRenderSize;
        var margin  = ViewerConstants.MapViewMargin;

        using var pen = new Pen(Color.FromArgb(80, Color.White));

        for (var x = 0; x <= _tileMap.Width; x++)
        {
            var px = margin + x * dstSize;
            g.DrawLine(pen, px, margin, px, margin + _tileMap.Height * dstSize);
        }

        for (var y = 0; y <= _tileMap.Height; y++)
        {
            var py = margin + y * dstSize;
            g.DrawLine(pen, margin, py, margin + _tileMap.Width * dstSize, py);
        }
    }

    private void DrawPreview(Graphics g)
    {
        if (!_showPreview) return;
        if (GetUsableTileset() == null) return;
        if (_hoverTile.X < 0 || _hoverTile.Y < 0) return;

        if (PreviewMetaTile != null)
        {
            DrawMetaTilePreview(g, PreviewMetaTile);
            return;
        }

        DrawTilePreview(g);
    }

    private void DrawTilePreview(Graphics g)
    {
        var tileset = GetUsableTileset();
        if (tileset == null || PreviewTileId < 0) return;

        var srcSize = MapConstants.DefaultTileSize;   // 16: 画像の切り出しサイズ
        var dstSize = ViewerConstants.TileRenderSize; // 32: 画面上の描画サイズ
        var margin  = ViewerConstants.MapViewMargin;
        var tilesPerRow = tileset.Width / srcSize;

        var sx = (PreviewTileId % tilesPerRow) * srcSize;
        var sy = (PreviewTileId / tilesPerRow) * srcSize;

        var srcRect = new Rectangle(sx, sy, srcSize, srcSize);
        var dstRect = new Rectangle(
            margin + _hoverTile.X * dstSize,
            margin + _hoverTile.Y * dstSize,
            dstSize,
            dstSize
        );

        DrawImageTransparent(g, srcRect, dstRect);

        using var pen = new Pen(Color.Yellow, 2);
        g.DrawRectangle(pen, dstRect);
    }

    private void DrawMetaTilePreview(Graphics g, MetaTile metaTile)
    {
        var tileset = GetUsableTileset();
        if (tileset == null) return;

        var srcSize = MapConstants.DefaultTileSize;
        var dstSize = ViewerConstants.TileRenderSize;
        var margin = ViewerConstants.MapViewMargin;
        var tilesPerRow = Math.Max(1, tileset.Width / srcSize);

        var minX = int.MaxValue;
        var minY = int.MaxValue;
        var maxX = int.MinValue;
        var maxY = int.MinValue;

        for (var y = 0; y < metaTile.Height; y++)
        {
            for (var x = 0; x < metaTile.Width; x++)
            {
                var tileId = metaTile.GetTile(x, y);

                if (tileId == MetaTile.EmptyTile)
                    continue;

                var mapX = _hoverTile.X + x;
                var mapY = _hoverTile.Y + y;

                // プレビューも実配置と同じく範囲外は無視する。
                if (!IsInside(mapX, mapY))
                    continue;

                var sx = (tileId % tilesPerRow) * srcSize;
                var sy = (tileId / tilesPerRow) * srcSize;

                var srcRect = new Rectangle(sx, sy, srcSize, srcSize);
                var dstRect = new Rectangle(
                    margin + mapX * dstSize,
                    margin + mapY * dstSize,
                    dstSize,
                    dstSize
                );

                DrawImageTransparent(g, srcRect, dstRect);

                minX = Math.Min(minX, dstRect.Left);
                minY = Math.Min(minY, dstRect.Top);
                maxX = Math.Max(maxX, dstRect.Right);
                maxY = Math.Max(maxY, dstRect.Bottom);
            }
        }

        if (minX == int.MaxValue)
            return;

        using var pen = new Pen(Color.Yellow, 2);
        g.DrawRectangle(
            pen,
            new Rectangle(minX, minY, maxX - minX - 1, maxY - minY - 1)
        );
    }

    private void DrawImageTransparent(Graphics g, Rectangle srcRect, Rectangle dstRect)
    {
        var tileset = GetUsableTileset();
        if (tileset == null) return;

        using var attr = new System.Drawing.Imaging.ImageAttributes();

        var matrix = new System.Drawing.Imaging.ColorMatrix
        {
            Matrix33 = 0.5f
        };

        attr.SetColorMatrix(matrix);

        g.DrawImage(
            tileset,
            dstRect,
            srcRect.X,
            srcRect.Y,
            srcRect.Width,
            srcRect.Height,
            GraphicsUnit.Pixel,
            attr
        );
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
        }

        base.Dispose(disposing);
    }

    // =========================
    // 入力
    // =========================
    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);

        if (_tileMap == null) return;

        if (e.Button == MouseButtons.Left &&
            TryHitAdjacentNavigationButton(e.Location, out var direction))
        {
            var hasAdjacent = _adjacentState.HasAdjacent(direction);

            AdjacentNavigationRequested?.Invoke(
                this,
                new AdjacentNavigationRequestedEventArgs(direction, hasAdjacent)
            );

            return;
        }

        var (x, y) = ScreenToTile(e.X, e.Y);

        if (!IsInside(x, y)) return;

        var isAlt = (ModifierKeys & Keys.Alt) != 0;
        var isShift = (ModifierKeys & Keys.Shift) != 0;

        // MetaTileなど、外部側で左クリックを処理する特殊ブラシの場合、
        // MapViewControl内部のTool処理へ入力を渡さない。
        if (e.Button == MouseButtons.Left &&
            ShouldSuppressToolInput?.Invoke() == true)
        {
            return;
        }

        // ========================
        // ① スポイト（最優先）
        // ========================
        if (e.Button == MouseButtons.Right || isAlt)
        {
            PickerTool?.OnMouseDown(x, y);
            return;
        }

        // ========================
        // ② 塗りつぶし
        // ========================
        if (isShift)
        {
            FillTool?.OnMouseDown(x, y);
            Invalidate();
            return;
        }

        // ========================
        // ③ 通常ツール
        // ========================
        _toolManager?.CurrentTool?.OnMouseDown(x, y);
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);

        if (_tileMap == null) return;

        var (x, y) = ScreenToTile(e.X, e.Y);

        if (!IsInside(x, y))
        {
            Cursor = Cursors.Default;
            _hoverTile = new Point(-1, -1);
            Invalidate();
            return;
        }

        var newHover = new Point(x, y);
        if (_hoverTile != newHover)
        {
            _hoverTile = newHover;
            Invalidate();
        }

        var currentTool = _toolManager?.CurrentTool;
        if (e.Button == MouseButtons.Left)
        {
            if (ShouldSuppressToolInput?.Invoke() != true)
            {
                currentTool?.OnMouseMove(x, y);
            }
        }
        Invalidate();
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);

        if (_tileMap == null) return;

        var (x, y) = ScreenToTile(e.X, e.Y);

        var isAlt = (ModifierKeys & Keys.Alt) != 0;

        // 範囲外ドロップを許可するため、ここではチェックしない
        //if (!IsInside(x, y)) return;

        if (e.Button == MouseButtons.Right || isAlt)
        {
            PickerTool?.OnMouseUp(x, y);
            return;
        }

        if (e.Button == MouseButtons.Left &&
            ShouldSuppressToolInput?.Invoke() == true)
        {
            return;
        }

        _toolManager?.CurrentTool?.OnMouseUp(x, y);
    }

    // =========================
    // 補助
    // =========================
    private static (int x, int y) ScreenToTile(int px, int py)
    {
        var dstSize = ViewerConstants.TileRenderSize;
        var margin  = ViewerConstants.MapViewMargin;
        return ((px - margin) / dstSize, (py - margin) / dstSize);
    }

    public bool TryScreenToTile(int px, int py, out int x, out int y)
    {
        (x, y) = ScreenToTile(px, py);
        return IsInside(x, y);
    }

    private bool IsInside(int x, int y)
    {
        return _tileMap != null &&
               x >= 0 && x < _tileMap.Width &&
               y >= 0 && y < _tileMap.Height;
    }

    private void DrawPlusCursor(Graphics g)
    {
        var pos = PointToClient(Cursor.Position);

        var size = 5;
        var offset = 24; // カーソルの位置よりも少し右下に描画するためのオフセット

        using var pen = new Pen(Color.White, 2);

        g.DrawLine(pen, pos.X - size + offset, pos.Y + offset, pos.X + size + offset, pos.Y + offset);
        g.DrawLine(pen, pos.X + offset, pos.Y - size + offset, pos.X + offset, pos.Y + size + offset);
    }

    public void UpdateCursor()
    {
        var isShift = (ModifierKeys & Keys.Shift) != 0;

        var (x, y) = _hoverTile.X >= 0 ? (_hoverTile.X, _hoverTile.Y) : (-1, -1);

        if (x < 0 || y < 0)
        {
            Cursor = Cursors.Default;
            return;
        }

        var currentTool = _toolManager?.CurrentTool;

        // バケツ（Pen限定）
        if (isShift && currentTool is PenTool)
        {
            Cursor = FillTool?.GetCursor(x, y) ?? Cursors.Hand;
            return;
        }

        Cursor = currentTool?.GetCursor(x, y) ?? Cursors.Default;
    }

    private void DrawAdjacentNavigationButtons(Graphics g)
    {
        foreach (var direction in new[]
        {
            PageDirection.Up,
            PageDirection.Down,
            PageDirection.Left,
            PageDirection.Right
        })
        {
            var rect = GetAdjacentNavigationButtonRect(direction);
            if (rect.IsEmpty)
                continue;

            var hasAdjacent = _adjacentState.HasAdjacent(direction);
            var text = hasAdjacent
                ? GetDirectionArrowText(direction)
                : "＋";

            using var backBrush = new SolidBrush(
                hasAdjacent
                    ? Color.FromArgb(90, 90, 110)
                    : Color.FromArgb(70, 100, 70)
            );

            using var borderPen = new Pen(Color.FromArgb(180, Color.White));
            using var textBrush = new SolidBrush(Color.White);

            g.FillEllipse(backBrush, rect);
            g.DrawEllipse(borderPen, rect);

            using var font = new Font("Yu Gothic UI", 10f, FontStyle.Bold);

            var format = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            };

            g.DrawString(text, font, textBrush, rect, format);
        }
    }

    private static string GetDirectionArrowText(PageDirection direction)
    {
        return direction switch
        {
            PageDirection.Up => "▲",
            PageDirection.Down => "▼",
            PageDirection.Left => "◀",
            PageDirection.Right => "▶",
            _ => string.Empty
        };
    }

    private Rectangle GetAdjacentNavigationButtonRect(PageDirection direction)
    {
        if (_tileMap == null)
            return Rectangle.Empty;

        var margin = ViewerConstants.MapViewMargin;
        var tileSize = ViewerConstants.TileRenderSize;

        var mapWidth = _tileMap.Width * tileSize;
        var mapHeight = _tileMap.Height * tileSize;

        var mapLeft = margin;
        var mapTop = margin;
        var mapRight = mapLeft + mapWidth;
        var mapBottom = mapTop + mapHeight;

        const int buttonSize = 24;

        return direction switch
        {
            PageDirection.Up => new Rectangle(
                mapLeft + mapWidth / 2 - buttonSize / 2,
                (margin - buttonSize) / 2,
                buttonSize,
                buttonSize),

            PageDirection.Down => new Rectangle(
                mapLeft + mapWidth / 2 - buttonSize / 2,
                mapBottom + (margin - buttonSize) / 2,
                buttonSize,
                buttonSize),

            PageDirection.Left => new Rectangle(
                (margin - buttonSize) / 2,
                mapTop + mapHeight / 2 - buttonSize / 2,
                buttonSize,
                buttonSize),

            PageDirection.Right => new Rectangle(
                mapRight + (margin - buttonSize) / 2,
                mapTop + mapHeight / 2 - buttonSize / 2,
                buttonSize,
                buttonSize),

            _ => Rectangle.Empty
        };
    }

    private bool TryHitAdjacentNavigationButton(Point point, out PageDirection direction)
    {
        foreach (var dir in new[]
        {
            PageDirection.Up,
            PageDirection.Down,
            PageDirection.Left,
            PageDirection.Right
        })
        {
            if (GetAdjacentNavigationButtonRect(dir).Contains(point))
            {
                direction = dir;
                return true;
            }
        }

        direction = default;
        return false;
    }
}

public sealed class MapAdjacentState
{
    public byte Up { get; init; } = 0xFF;
    public byte Down { get; init; } = 0xFF;
    public byte Left { get; init; } = 0xFF;
    public byte Right { get; init; } = 0xFF;

    public byte GetRoomId(PageDirection direction)
    {
        return direction switch
        {
            PageDirection.Up => Up,
            PageDirection.Down => Down,
            PageDirection.Left => Left,
            PageDirection.Right => Right,
            _ => 0xFF
        };
    }

    public bool HasAdjacent(PageDirection direction)
    {
        return GetRoomId(direction) != 0xFF;
    }
}

public sealed class AdjacentNavigationRequestedEventArgs : EventArgs
{
    public PageDirection Direction { get; }
    public bool HasAdjacentPage { get; }

    public AdjacentNavigationRequestedEventArgs(
        PageDirection direction,
        bool hasAdjacentPage)
    {
        Direction = direction;
        HasAdjacentPage = hasAdjacentPage;
    }
}
