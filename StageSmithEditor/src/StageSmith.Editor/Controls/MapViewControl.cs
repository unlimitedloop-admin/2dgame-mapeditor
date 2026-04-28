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

    public void SetTileMap(TileMap map)
    {
        _tileMap = map;
        Invalidate();
    }

    public void SetTileset(Bitmap tileset)
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
        SelectionTool?.DrawOverlay(g, MapConstants.TilePixelSize);
        SelectionTool?.DrawMovingOverlay(g, MapConstants.TilePixelSize, _tileset);

        // Extended add plus cursor at copy mode
        if (SelectionTool?.IsCopyModeActive() == true)
        {
            DrawPlusCursor(g);
        }
    }

    private void DrawTiles(Graphics g)
    {
        if (_tileMap == null || _tileset == null) return;

        var tileSize = MapConstants.TilePixelSize;
        var tilesPerRow = _tileset.Width / tileSize;

        for (var y = 0; y < _tileMap.Height; y++)
        {
            for (var x = 0; x < _tileMap.Width; x++)
            {
                var tileId = _tileMap.GetTile(x, y);

                var sx = (tileId % tilesPerRow) * tileSize;
                var sy = (tileId / tilesPerRow) * tileSize;

                var srcRect = new Rectangle(sx, sy, tileSize, tileSize);
                var dstRect = new Rectangle(x * tileSize, y * tileSize, tileSize, tileSize);

                g.DrawImage(_tileset, dstRect, srcRect, GraphicsUnit.Pixel);
            }
        }
    }

    private void DrawGrid(Graphics g)
    {
        if (_tileMap == null) return;

        var tileSize = MapConstants.TilePixelSize;

        using var pen = new Pen(Color.FromArgb(80, Color.White));

        for (var x = 0; x <= _tileMap.Width; x++)
        {
            var px = x * tileSize;
            g.DrawLine(pen, px, 0, px, _tileMap.Height * tileSize);
        }

        for (var y = 0; y <= _tileMap.Height; y++)
        {
            var py = y * tileSize;
            g.DrawLine(pen, 0, py, _tileMap.Width * tileSize, py);
        }
    }

    private void DrawPreview(Graphics g)
    {
        if (!_showPreview) return;
        if (_tileset == null || PreviewTileId < 0) return;
        if (_hoverTile.X < 0 || _hoverTile.Y < 0) return;

        var tileSize = MapConstants.TilePixelSize;
        var tilesPerRow = _tileset.Width / tileSize;

        var sx = (PreviewTileId % tilesPerRow) * tileSize;
        var sy = (PreviewTileId / tilesPerRow) * tileSize;

        var srcRect = new Rectangle(sx, sy, tileSize, tileSize);
        var dstRect = new Rectangle(
            _hoverTile.X * tileSize,
            _hoverTile.Y * tileSize,
            tileSize,
            tileSize
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

        if (e.Button == MouseButtons.Left)
        {
            _toolManager?.CurrentTool?.OnMouseDown(x, y);
        }
        else if (e.Button == MouseButtons.Right)
        {
            PickerTool?.OnMouseDown(x, y);
        }
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
        Cursor = currentTool?.GetCursor(x, y) ?? Cursors.Default;

        currentTool?.OnMouseMove(x, y);
        Invalidate();
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);

        if (_tileMap == null) return;

        var (x, y) = ScreenToTile(e.X, e.Y);

        // 範囲外ドロップを許可するため、ここではチェックしない
        //if (!IsInside(x, y)) return;

        _toolManager?.CurrentTool?.OnMouseUp(x, y);
    }

    // =========================
    // 補助
    // =========================

    private static (int x, int y) ScreenToTile(int px, int py)
    {
        var tileSize = MapConstants.TilePixelSize;
        return (px / tileSize, py / tileSize);
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
}
