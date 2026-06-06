using System.ComponentModel;
using StageSmith.Core.Constants;
using StageSmith.Core.Models;
using StageSmith.Editor.Tools;

namespace StageSmith.Editor.Controls;

public class MapViewControl : DoubleBufferedPanel
{
    private TileMap? _tileMap;
    private Bitmap? _tileset;

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
        _tileset = tileset;
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

        if (_tileset != null)
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
        SelectionTool?.DrawMovingOverlay(g, ViewerConstants.TileRenderSize, _tileset, ViewerConstants.MapViewMargin);

        // Extended add plus cursor at copy mode
        if (SelectionTool?.IsCopyModeActive() == true)
        {
            DrawPlusCursor(g);
        }
    }

    private void DrawTiles(Graphics g)
    {
        if (_tileMap == null || _tileset == null) return;

        var srcSize = MapConstants.DefaultTileSize;   // 16: 画像の切り出しサイズ
        var dstSize = ViewerConstants.TileRenderSize; // 32: 画面上の描画サイズ
        var margin  = ViewerConstants.MapViewMargin;  // 32: 上下左右マージン
        var tilesPerRow = _tileset.Width / srcSize;

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

                g.DrawImage(_tileset, dstRect, srcRect, GraphicsUnit.Pixel);
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
        if (_tileset == null || PreviewTileId < 0) return;
        if (_hoverTile.X < 0 || _hoverTile.Y < 0) return;

        var srcSize = MapConstants.DefaultTileSize;   // 16: 画像の切り出しサイズ
        var dstSize = ViewerConstants.TileRenderSize; // 32: 画面上の描画サイズ
        var margin  = ViewerConstants.MapViewMargin;
        var tilesPerRow = _tileset.Width / srcSize;

        var sx = (PreviewTileId % tilesPerRow) * srcSize;
        var sy = (PreviewTileId / tilesPerRow) * srcSize;

        var srcRect = new Rectangle(sx, sy, srcSize, srcSize);
        var dstRect = new Rectangle(
            margin + _hoverTile.X * dstSize,
            margin + _hoverTile.Y * dstSize,
            dstSize,
            dstSize
        );

        using var attr = new System.Drawing.Imaging.ImageAttributes();

        var matrix = new System.Drawing.Imaging.ColorMatrix
        {
            Matrix33 = 0.5f
        };

        attr.SetColorMatrix(matrix);

        g.DrawImage(
            _tileset,
            dstRect,
            srcRect.X,
            srcRect.Y,
            srcRect.Width,
            srcRect.Height,
            GraphicsUnit.Pixel,
            attr
        );

        using var pen = new Pen(Color.Yellow, 2);
        g.DrawRectangle(pen, dstRect);
    }

    // =========================
    // 入力
    // =========================
    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);

        if (_tileMap == null) return;

        var (x, y) = ScreenToTile(e.X, e.Y);

        if (!IsInside(x, y)) return;

        var isAlt = (ModifierKeys & Keys.Alt) != 0;
        var isShift = (ModifierKeys & Keys.Shift) != 0;

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
            currentTool?.OnMouseMove(x, y);
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
}
