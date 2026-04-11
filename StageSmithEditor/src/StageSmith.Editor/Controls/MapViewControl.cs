using StageSmith.Core.Constants;
using StageSmith.Core.Models;
using StageSmith.Editor.Tools;
using System.ComponentModel;
using System.Drawing.Imaging;

namespace StageSmith.Editor.Controls;

public class MapViewControl : DoubleBufferedPanel
{
    private TileMap? _tileMap;
    private Bitmap? _tileset;

    private bool _showGrid = true;
    private bool _showPreview = false;
    private bool _isMouseDown = false;

    private Point _hoverTile = new(-1, -1);

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public ITool? CurrentTool { get; set; }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public ITool? PickerTool { get; set; }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int PreviewTileId { get; set; } = -1;

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool ShowPreview
    {
        get => _showPreview;
        set
        {
            if (_showPreview == value) return;

            _showPreview = value;
            Invalidate(); // 自動更新🔥
        }
    }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public ITool? FillTool { get; set; }

    public MapViewControl()
    {
        DoubleBuffered = true;
        ResizeRedraw = true;
    }

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

        // 半透明描画
        using var attr = new ImageAttributes();

        var matrix = new ColorMatrix
        {
            Matrix33 = 0.5f // ←透明度🔥
        };

        attr.SetColorMatrix(matrix, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);

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

        // 枠（任意）
        using var pen = new Pen(Color.Yellow, 2);
        g.DrawRectangle(pen, dstRect);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        if (_tileMap == null) return;

        var g = e.Graphics;
        g.Clear(this.BackColor);

        // --- タイル描画 ---
        if (_tileset != null)
        {
            DrawTiles(g);
        }

        // --- グリッド ---
        if (_showGrid)
        {
            DrawGrid(g);
        }

        // --- プレビュー ---
        if (_showPreview)
        {
            DrawPreview(g);
        }
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);

        if (_tileMap == null) return;

        var tileSize = MapConstants.TilePixelSize;
        var x = e.X / tileSize;
        var y = e.Y / tileSize;

        if (x < 0 || x >= _tileMap.Width ||
            y < 0 || y >= _tileMap.Height)
            return;

        // 🔥 Shift押されてるか判定
        if ((ModifierKeys & Keys.Shift) != 0)
        {
            // Fill発動
            FillTool?.OnMouseDown(x, y);
            return;
        }

        if (e.Button == MouseButtons.Left)
        {
            _isMouseDown = true;
            CurrentTool?.OnMouseDown(x, y);
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

        var tileSize = MapConstants.TilePixelSize;
        var x = e.X / tileSize;
        var y = e.Y / tileSize;

        if (x < 0 || x >= _tileMap.Width ||
            y < 0 || y >= _tileMap.Height)
        {
            _hoverTile = new Point(-1, -1);
            Invalidate();   // 範囲外でもプレビューを消すために再描画
            return; 
        }
        var newHover = new Point(x, y);

        if (_isMouseDown)
        {
            CurrentTool?.OnMouseMove(x, y);
        }

        if (_hoverTile != newHover)  // 毎フレーム更新回避（軽量化）
        {
            _hoverTile = newHover;
            Invalidate();   // ホバー更新
        }
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);

        if (_tileMap != null)
        {
            var tileSize = MapConstants.TilePixelSize;
            var x = e.X / tileSize;
            var y = e.Y / tileSize;

            CurrentTool?.OnMouseUp(x, y);
        }

        _isMouseDown = false;
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);

        _hoverTile = new Point(-1, -1);
        Invalidate();   // コントロールから離れたらプレビューを消す
    }
}
