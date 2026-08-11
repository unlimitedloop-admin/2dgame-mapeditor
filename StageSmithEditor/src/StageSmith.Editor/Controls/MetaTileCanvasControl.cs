using StageSmith.Core.Constants;
using StageSmith.Core.Models;
using StageSmith.Editor.Utilities;

namespace StageSmith.Editor.Controls;

/// <summary>
/// メタタイルを表示するキャンバスコントロールです。
/// </summary>
public sealed class MetaTileCanvasControl : DoubleBufferedPanel
{
    private MetaTile? _metaTile;
    private readonly SafeTilesetHolder _tilesetHolder = new();
    private int _selectedTileId = -1;

    private const int CellSize = 32;
    private const int MarginSize = 16;

    /// <summary>
    /// タイルが選択されたときに発生します。
    /// </summary>
    public event Action<int>? TilePicked;

    /// <summary>
    /// 編集が開始されたときに発生します。
    /// </summary>
    public event Action? EditStarted;

    /// <summary>
    /// タイルのペイントが要求されたときに発生します。
    /// </summary>
    public event Action<int, int, byte>? TilePaintRequested;

    /// <summary>
    /// 編集が終了したときに発生します。
    /// </summary>
    public event Action? EditFinished;

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
        _tilesetHolder.Replace(tileset);
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
        var tileset = _tilesetHolder.Current;

        if (_metaTile == null || tileset == null)
            return;

        var srcSize = MapConstants.DefaultTileSize;
        var tilesPerRow = tileset.Width / srcSize;

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

                g.DrawImage(tileset, dstRect, srcRect, GraphicsUnit.Pixel);
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

        if (e.Button == MouseButtons.Left)
        {
            EditStarted?.Invoke();
            RequestPaint(e);
        }
        else if (e.Button == MouseButtons.Right)
        {
            EditStarted?.Invoke();
            RequestErase(e);
        }
        else if (e.Button == MouseButtons.Middle)
        {
            PickTile(e);
        }
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);

        if (e.Button == MouseButtons.Left)
        {
            RequestPaint(e);
        }
        else if (e.Button == MouseButtons.Right)
        {
            RequestErase(e);
        }
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);

        if (e.Button == MouseButtons.Left || e.Button == MouseButtons.Right)
        {
            EditFinished?.Invoke();
        }
    }

    private void RequestPaint(MouseEventArgs e)
    {
        if (_metaTile == null)
            return;

        if (_selectedTileId < 0)
            return;

        var x = (e.X - MarginSize) / CellSize;
        var y = (e.Y - MarginSize) / CellSize;

        if (x < 0 || x >= _metaTile.Width || y < 0 || y >= _metaTile.Height)
            return;

        TilePaintRequested?.Invoke(x, y, (byte)_selectedTileId);
    }

    private void RequestErase(MouseEventArgs e)
    {
        if (_metaTile == null)
            return;

        var x = (e.X - MarginSize) / CellSize;
        var y = (e.Y - MarginSize) / CellSize;

        if (x < 0 || x >= _metaTile.Width || y < 0 || y >= _metaTile.Height)
            return;

        TilePaintRequested?.Invoke(x, y, MetaTile.EmptyTile);
    }

    private void PickTile(MouseEventArgs e)
    {
        if (_metaTile == null)
            return;

        var x = (e.X - MarginSize) / CellSize;
        var y = (e.Y - MarginSize) / CellSize;

        if (x < 0 || x >= _metaTile.Width || y < 0 || y >= _metaTile.Height)
            return;

        var tileId = _metaTile.GetTile(x, y);

        if (tileId != MetaTile.EmptyTile)
        {
            TilePicked?.Invoke(tileId);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _tilesetHolder.Dispose();
        }

        base.Dispose(disposing);
    }
}
