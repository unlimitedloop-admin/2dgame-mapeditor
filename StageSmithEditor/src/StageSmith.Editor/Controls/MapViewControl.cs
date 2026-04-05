using StageSmith.Core.Constants;
using StageSmith.Core.Models;

namespace StageSmith.Editor.Controls;

public class MapViewControl : Panel
{
    private TileMap? _tileMap;
    private Bitmap? _tileset;

    private bool _showGrid = true;

    private bool _isMouseDown = false;

    public MapViewControl()
    {
        this.DoubleBuffered = true;
        this.ResizeRedraw = true;
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

    public event Action<int, int>? TilePaintRequested;
    public event Action<int>? TilePicked;

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        if (e.Graphics == null || _tileMap == null || _tileset == null) return;

        var g = e.Graphics;

        g.Clear(this.BackColor);

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

        if (_showGrid)
        {
            DrawGrid(g);
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

        if (e.Button == MouseButtons.Left)
        {
            _isMouseDown = true;
            TilePaintRequested?.Invoke(x, y);
        }
        else if (e.Button == MouseButtons.Right)
        {
            int tileId = _tileMap.GetTile(x, y);
            TilePicked?.Invoke(tileId);
        }
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);

        if (!_isMouseDown || _tileMap == null) return;

        var tileSize = MapConstants.TilePixelSize;
        var x = e.X / tileSize;
        var y = e.Y / tileSize;

        if (x < 0 || x >= _tileMap.Width ||
            y < 0 || y >= _tileMap.Height)
            return;

        TilePaintRequested?.Invoke(x, y);
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        _isMouseDown = false;
    }
}
