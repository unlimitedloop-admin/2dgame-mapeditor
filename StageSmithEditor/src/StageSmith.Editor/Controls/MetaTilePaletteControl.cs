using StageSmith.Core.Constants;
using StageSmith.Core.Models;
using StageSmith.Editor.Utilities;

namespace StageSmith.Editor.Controls;

/// <summary>
/// 保存済みメタタイルをサムネイル表示し、選択できるパレット。
/// メタタイル名などの詳細は ToolTip で表示する。
/// </summary>
public sealed class MetaTilePaletteControl : DoubleBufferedPanel
{
    private Stage? _stage;
    private readonly SafeTilesetHolder _tilesetHolder = new();

    private readonly ToolTip _toolTip = new();
    private readonly List<MetaTileLayoutItem> _items = [];
    private readonly ContextMenuStrip _contextMenu;

    private int? _hoverMetaTileId;
    private int? _selectedMetaTileId;

    private const int ItemSize = 76;
    private const int ItemSpacing = 6;
    private const int ItemPadding = 6;
    private const int BottomPadding = 24;
    private const int MinPreviewTileSize = 4;

    public event Action<MetaTile>? MetaTileSelected;
    public event Action? MetaTileEditorRequested;

    public int? SelectedMetaTileId => _selectedMetaTileId;

    public MetaTilePaletteControl()
    {
        DoubleBuffered = true;
        AutoScroll = true;
        ResizeRedraw = true;
        BackColor = Color.FromArgb(245, 245, 245);

        _contextMenu = new ContextMenuStrip();
        _contextMenu.Items.Add("メタタイルエディタを起動...", null, (_, _) => MetaTileEditorRequested?.Invoke());
        ContextMenuStrip = _contextMenu;
    }

    public void SetStage(Stage? stage)
    {
        _stage = stage;

        if (_selectedMetaTileId.HasValue &&
            _stage?.FindMetaTile(_selectedMetaTileId.Value) is null)
        {
            _selectedMetaTileId = null;
        }

        RebuildLayout();
        Invalidate();
    }

    public void SetTileset(Bitmap? tileset)
    {
        _tilesetHolder.Replace(tileset);
        Invalidate();
    }

    public void RefreshPalette()
    {
        RebuildLayout();
        Invalidate();
    }

    public void SetSelected(MetaTile? metaTile)
    {
        _selectedMetaTileId = metaTile?.Id;
        Invalidate();
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        RebuildLayout();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        var g = e.Graphics;
        g.Clear(BackColor);

        g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
        g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;

        if (_stage == null || _stage.MetaTiles.Count == 0)
        {
            DrawEmptyMessage(g);
            return;
        }

        var offset = AutoScrollPosition;

        foreach (var item in _items)
        {
            var rect = item.Bounds;
            rect.Offset(offset);

            DrawItemBackground(g, rect, item.MetaTile);
            DrawMetaTilePreview(g, rect, item.MetaTile);
        }
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);

        if (e.Button != MouseButtons.Left)
            return;

        var metaTile = HitTest(e.Location);
        if (metaTile == null)
            return;

        _selectedMetaTileId = metaTile.Id;
        MetaTileSelected?.Invoke(metaTile);
        Invalidate();
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);

        var metaTile = HitTest(e.Location);
        var metaTileId = metaTile?.Id;

        if (_hoverMetaTileId == metaTileId)
            return;

        _hoverMetaTileId = metaTileId;

        if (metaTile == null)
        {
            _toolTip.SetToolTip(this, string.Empty);
            return;
        }

        _toolTip.SetToolTip(
            this,
            $"ID: {metaTile.Id:D3}\nName: {metaTile.Name}\nSize: {metaTile.Width} x {metaTile.Height}"
        );
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        _hoverMetaTileId = null;
        _toolTip.SetToolTip(this, string.Empty);
    }

    private void RebuildLayout()
    {
        _items.Clear();

        if (_stage == null)
        {
            AutoScrollMinSize = Size.Empty;
            return;
        }

        var viewportWidth = Math.Max(1, ClientSize.Width - SystemInformation.VerticalScrollBarWidth);
        var columns = Math.Max(1, viewportWidth / (ItemSize + ItemSpacing));

        for (var i = 0; i < _stage.MetaTiles.Count; i++)
        {
            var col = i % columns;
            var row = i / columns;

            var x = ItemSpacing + col * (ItemSize + ItemSpacing);
            var y = ItemSpacing + row * (ItemSize + ItemSpacing);

            _items.Add(new MetaTileLayoutItem(
                _stage.MetaTiles[i],
                new Rectangle(x, y, ItemSize, ItemSize)
            ));
        }

        var rowCount = _stage.MetaTiles.Count == 0
            ? 0
            : ((_stage.MetaTiles.Count - 1) / columns) + 1;

        AutoScrollMinSize = new Size(
            columns * (ItemSize + ItemSpacing) + ItemSpacing,
            rowCount * (ItemSize + ItemSpacing) + ItemSpacing + BottomPadding
        );
    }

    private MetaTile? HitTest(Point location)
    {
        var virtualPoint = new Point(
            location.X - AutoScrollPosition.X,
            location.Y - AutoScrollPosition.Y
        );

        foreach (var item in _items)
        {
            if (item.Bounds.Contains(virtualPoint))
                return item.MetaTile;
        }

        return null;
    }

    private void DrawEmptyMessage(Graphics g)
    {
        using var brush = new SolidBrush(Color.FromArgb(120, Color.Black));

        g.DrawString(
            "No MetaTiles",
            Font,
            brush,
            new PointF(8, 8)
        );
    }

    private void DrawItemBackground(Graphics g, Rectangle rect, MetaTile metaTile)
    {
        var isSelected = _selectedMetaTileId == metaTile.Id;

        using var backBrush = new SolidBrush(isSelected
            ? Color.FromArgb(230, 245, 255)
            : Color.FromArgb(255, 255, 255));

        using var borderPen = new Pen(isSelected
            ? Color.FromArgb(220, Color.Red)
            : Color.FromArgb(140, Color.Gray), isSelected ? 2 : 1);

        g.FillRectangle(backBrush, rect);
        g.DrawRectangle(borderPen, new Rectangle(
            rect.X,
            rect.Y,
            rect.Width - 1,
            rect.Height - 1
        ));
    }

    private void DrawMetaTilePreview(Graphics g, Rectangle itemRect, MetaTile metaTile)
    {
        var previewRect = Rectangle.Inflate(itemRect, -ItemPadding, -ItemPadding);

        var tileset = _tilesetHolder.Current;

        if (tileset == null)
        {
            DrawFallbackLabel(g, previewRect, metaTile);
            return;
        }

        var srcSize = MapConstants.DefaultTileSize;
        var tilesPerRow = Math.Max(1, tileset.Width / srcSize);

        var tileSize = Math.Min(
            previewRect.Width / Math.Max(1, metaTile.Width),
            previewRect.Height / Math.Max(1, metaTile.Height)
        );

        tileSize = Math.Max(MinPreviewTileSize, tileSize);

        var drawWidth = metaTile.Width * tileSize;
        var drawHeight = metaTile.Height * tileSize;

        var originX = previewRect.X + (previewRect.Width - drawWidth) / 2;
        var originY = previewRect.Y + (previewRect.Height - drawHeight) / 2;

        for (var y = 0; y < metaTile.Height; y++)
        {
            for (var x = 0; x < metaTile.Width; x++)
            {
                var tileId = metaTile.GetTile(x, y);

                if (tileId == MetaTile.EmptyTile)
                    continue;

                var sx = (tileId % tilesPerRow) * srcSize;
                var sy = (tileId / tilesPerRow) * srcSize;

                var srcRect = new Rectangle(sx, sy, srcSize, srcSize);
                var dstRect = new Rectangle(
                    originX + x * tileSize,
                    originY + y * tileSize,
                    tileSize,
                    tileSize
                );

                g.DrawImage(tileset, dstRect, srcRect, GraphicsUnit.Pixel);
            }
        }
    }

    private void DrawFallbackLabel(Graphics g, Rectangle rect, MetaTile metaTile)
    {
        using var brush = new SolidBrush(Color.FromArgb(90, Color.Black));

        var text = $"#{metaTile.Id:D3}";
        var size = g.MeasureString(text, Font);

        g.DrawString(
            text,
            Font,
            brush,
            rect.X + (rect.Width - size.Width) / 2,
            rect.Y + (rect.Height - size.Height) / 2
        );
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _toolTip.Dispose();
            _tilesetHolder.Dispose();
        }

        base.Dispose(disposing);
    }

    private sealed record MetaTileLayoutItem(MetaTile MetaTile, Rectangle Bounds);
}
