using StageSmith.Core.Constants;
using StageSmith.Core.Models;

namespace StageSmith.Editor.Controls;

public sealed class MetaTileCanvasControl : DoubleBufferedPanel
{
    private MetaTile? _metaTile;
    private Bitmap? _tileset;
    private int _selectedTileId = -1;

    private const int CellSize = 32;
    private const int MarginSize = 16;

    public event Action<int>? TilePicked;
    public event Action? MetaTileChanged;

    public MetaTileCanvasControl()
    {
        DoubleBuffered = true;
        ResizeRedraw = true;
        BackColor = Color.FromArgb(32, 32, 32);
    }

    public void SetMetaTile(MetaTile? metaTile)
    {
        _metaTile = metaTile;
        UpdateSize();
        Invalidate();
    }

    public void SetTileset(Bitmap? tileset)
    {
        _tileset = tileset;
        Invalidate();
    }

    public void SetSelectedTile(int tileId)
    {
        _selectedTileId = tileId;
    }

    private void UpdateSize()
    {
        if (_metaTile == null)
        {
            Size = new Size(200, 200);
            return;
        }

        Size = new Size(
            MarginSize * 2 + _metaTile.Width * CellSize,
            MarginSize * 2 + _metaTile.Height * CellSize
        );
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        var g = e.Graphics;
        g.Clear(BackColor);

        if (_metaTile == null)
            return;

        g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
        g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;

        DrawTiles(g);
        DrawGrid(g);
    }

    private void DrawTiles(Graphics g)
    {
        if (_metaTile == null || _tileset == null)
            return;

        var srcSize = MapConstants.DefaultTileSize;
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
                    MarginSize + x * CellSize,
                    MarginSize + y * CellSize,
                    CellSize,
                    CellSize
                );

                g.DrawImage(_tileset, dstRect, srcRect, GraphicsUnit.Pixel);
            }
        }
    }

    private void DrawGrid(Graphics g)
    {
        if (_metaTile == null)
            return;

        using var pen = new Pen(Color.FromArgb(120, Color.White));

        for (var x = 0; x <= _metaTile.Width; x++)
        {
            var px = MarginSize + x * CellSize;
            g.DrawLine(
                pen,
                px,
                MarginSize,
                px,
                MarginSize + _metaTile.Height * CellSize
            );
        }

        for (var y = 0; y <= _metaTile.Height; y++)
        {
            var py = MarginSize + y * CellSize;
            g.DrawLine(
                pen,
                MarginSize,
                py,
                MarginSize + _metaTile.Width * CellSize,
                py
            );
        }
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        HandleMouse(e);
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);

        if (e.Button == MouseButtons.Left || e.Button == MouseButtons.Right)
        {
            HandleMouse(e);
        }
    }

    private void HandleMouse(MouseEventArgs e)
    {
        if (_metaTile == null)
            return;

        var x = (e.X - MarginSize) / CellSize;
        var y = (e.Y - MarginSize) / CellSize;

        if (x < 0 || x >= _metaTile.Width || y < 0 || y >= _metaTile.Height)
            return;

        if (e.Button == MouseButtons.Left)
        {
            if (_selectedTileId < 0)
                return;

            _metaTile.SetTile(x, y, (byte)_selectedTileId);
            MetaTileChanged?.Invoke();
            Invalidate();
        }
        else if (e.Button == MouseButtons.Right)
        {
            var tileId = _metaTile.GetTile(x, y);

            if (tileId != MetaTile.EmptyTile)
            {
                TilePicked?.Invoke(tileId);
            }
        }
    }
}
