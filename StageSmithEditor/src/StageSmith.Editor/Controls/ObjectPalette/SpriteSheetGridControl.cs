using StageSmith.Core.Models;
using StageSmith.Editor.Utilities;

namespace StageSmith.Editor.Controls;

/// <summary>
/// 画像シート全体をコマ単位のグリッドで表示し、1コマを選択するためのビュー。
/// 表示倍率は幅に収まる整数倍（1〜MaxScale）を自動で選ぶ。
/// </summary>
public sealed class SpriteSheetGridControl : DoubleBufferedPanel
{
    private const int SheetMargin = 6;
    private const int MaxScale = 3;

    private SpriteSheet? _sheet;
    private SpriteSheetImageCache? _imageCache;

    private int _scale = 1;
    private int _hoverTileIndex = -1;

    private readonly ToolTip _toolTip = new();

    public int SelectedTileIndex { get; private set; } = -1;

    /// <summary>コマがクリックで選択されたとき。</summary>
    public event Action<int>? TileSelected;

    /// <summary>コマがダブルクリックされたとき（テンプレート登録のショートカット）。</summary>
    public event Action<int>? TileActivated;

    public SpriteSheetGridControl()
    {
        DoubleBuffered = true;
        AutoScroll = true;
        ResizeRedraw = true;
        BackColor = Color.FromArgb(245, 245, 245);
    }

    public void SetSheet(SpriteSheet? sheet, SpriteSheetImageCache? imageCache)
    {
        if (!ReferenceEquals(_sheet, sheet))
            SelectedTileIndex = -1;

        _sheet = sheet;
        _imageCache = imageCache;
        _hoverTileIndex = -1;

        if (_sheet != null && !_sheet.IsValidTileIndex(SelectedTileIndex))
            SelectedTileIndex = -1;

        UpdateLayoutMetrics();
        Invalidate();
    }

    public void SetSelectedTile(int tileIndex)
    {
        SelectedTileIndex = _sheet != null && _sheet.IsValidTileIndex(tileIndex) ? tileIndex : -1;
        Invalidate();
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        UpdateLayoutMetrics();
    }

    private void UpdateLayoutMetrics()
    {
        if (_sheet == null)
        {
            AutoScrollMinSize = Size.Empty;
            return;
        }

        var sheetWidth = _sheet.TilesX * _sheet.TileWidth;
        var sheetHeight = _sheet.TilesY * _sheet.TileHeight;

        var available = Math.Max(1, ClientSize.Width - SheetMargin * 2 - SystemInformation.VerticalScrollBarWidth);
        _scale = Math.Clamp(available / Math.Max(1, sheetWidth), 1, MaxScale);

        AutoScrollMinSize = new Size(
            sheetWidth * _scale + SheetMargin * 2,
            sheetHeight * _scale + SheetMargin * 2);
    }

    private Rectangle GetCellRect(int tileIndex)
    {
        var sheet = _sheet!;
        var col = tileIndex % sheet.TilesX;
        var row = tileIndex / sheet.TilesX;

        return new Rectangle(
            SheetMargin + AutoScrollPosition.X + col * sheet.TileWidth * _scale,
            SheetMargin + AutoScrollPosition.Y + row * sheet.TileHeight * _scale,
            sheet.TileWidth * _scale,
            sheet.TileHeight * _scale);
    }

    private int HitTest(Point location)
    {
        if (_sheet == null) return -1;

        var x = location.X - SheetMargin - AutoScrollPosition.X;
        var y = location.Y - SheetMargin - AutoScrollPosition.Y;
        if (x < 0 || y < 0) return -1;

        var col = x / (_sheet.TileWidth * _scale);
        var row = y / (_sheet.TileHeight * _scale);
        if (col >= _sheet.TilesX || row >= _sheet.TilesY) return -1;

        return row * _sheet.TilesX + col;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        var g = e.Graphics;
        g.Clear(BackColor);
        g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
        g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;

        if (_sheet == null)
        {
            DrawMessage(g, "シートがありません。\n「＋」から画像シートを追加してください。");
            return;
        }

        var hasImage = _imageCache?.Get(_sheet) != null;

        for (var i = 0; i < _sheet.TileCount; i++)
        {
            var rect = GetCellRect(i);
            _imageCache?.DrawTile(g, _sheet, i, rect);
        }

        DrawGrid(g);

        if (_hoverTileIndex >= 0 && _hoverTileIndex != SelectedTileIndex)
        {
            using var hoverPen = new Pen(Color.FromArgb(160, Color.DodgerBlue), 1);
            var r = GetCellRect(_hoverTileIndex);
            g.DrawRectangle(hoverPen, r.X, r.Y, r.Width - 1, r.Height - 1);
        }

        if (SelectedTileIndex >= 0)
        {
            using var selectedPen = new Pen(Color.FromArgb(220, Color.Red), 2);
            var r = GetCellRect(SelectedTileIndex);
            g.DrawRectangle(selectedPen, r.X + 1, r.Y + 1, r.Width - 2, r.Height - 2);
        }

        if (!hasImage)
        {
            DrawMessage(g, $"画像を読み込めません。\n{_sheet.ImagePath}");
        }
    }

    private void DrawGrid(Graphics g)
    {
        var sheet = _sheet!;
        var origin = GetCellRect(0).Location;
        var cellW = sheet.TileWidth * _scale;
        var cellH = sheet.TileHeight * _scale;
        var totalW = sheet.TilesX * cellW;
        var totalH = sheet.TilesY * cellH;

        using var pen = new Pen(Color.FromArgb(70, Color.Gray));

        for (var x = 0; x <= sheet.TilesX; x++)
            g.DrawLine(pen, origin.X + x * cellW, origin.Y, origin.X + x * cellW, origin.Y + totalH);

        for (var y = 0; y <= sheet.TilesY; y++)
            g.DrawLine(pen, origin.X, origin.Y + y * cellH, origin.X + totalW, origin.Y + y * cellH);
    }

    private void DrawMessage(Graphics g, string text)
    {
        using var brush = new SolidBrush(Color.FromArgb(140, Color.Black));
        g.DrawString(text, Font, brush, new RectangleF(8, 8, Math.Max(1, ClientSize.Width - 16), ClientSize.Height - 16));
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        Focus();

        if (e.Button != MouseButtons.Left) return;

        var index = HitTest(e.Location);
        if (index < 0) return;

        SelectedTileIndex = index;
        Invalidate();
        TileSelected?.Invoke(index);
    }

    protected override void OnMouseDoubleClick(MouseEventArgs e)
    {
        base.OnMouseDoubleClick(e);

        if (e.Button != MouseButtons.Left) return;

        var index = HitTest(e.Location);
        if (index >= 0)
            TileActivated?.Invoke(index);
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);

        var index = HitTest(e.Location);
        if (index == _hoverTileIndex) return;

        _hoverTileIndex = index;
        _toolTip.SetToolTip(this, index >= 0 ? $"#{index}" : string.Empty);
        Invalidate();
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        _hoverTileIndex = -1;
        _toolTip.SetToolTip(this, string.Empty);
        Invalidate();
    }

    protected override void OnScroll(ScrollEventArgs se)
    {
        base.OnScroll(se);
        Invalidate();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _toolTip.Dispose();

        base.Dispose(disposing);
    }
}
