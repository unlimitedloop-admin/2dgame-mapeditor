using StageSmith.Core.Models;
using StageSmith.Editor.Utilities;

namespace StageSmith.Editor.Controls;

/// <summary>
/// 登録済みの配置用テンプレート（EntityTemplate）をサムネイル一覧で表示し、選択するためのビュー。
/// 見た目・操作感は MetaTilePaletteControl に合わせている。
/// </summary>
public sealed class EntityTemplateListControl : DoubleBufferedPanel
{
    private const int ThumbSize = 52;
    private const int LabelHeight = 16;
    private const int ItemWidth = ThumbSize + 8;
    private const int ItemHeight = ThumbSize + LabelHeight + 8;
    private const int ItemSpacing = 6;

    private EditorProject? _project;
    private SpriteSheetImageCache? _imageCache;

    private readonly List<TemplateLayoutItem> _items = [];
    private readonly ToolTip _toolTip = new();
    private readonly ContextMenuStrip _contextMenu = new();
    private readonly ToolStripMenuItem _editMenuItem;
    private readonly ToolStripMenuItem _removeMenuItem;

    private Guid? _hoverTemplateId;
    private EntityTemplate? _contextTarget;

    public Guid? SelectedTemplateId { get; private set; }

    public event Action<EntityTemplate>? TemplateSelected;
    public event Action<EntityTemplate>? EditRequested;
    public event Action<EntityTemplate>? RemoveRequested;

    public EntityTemplateListControl()
    {
        DoubleBuffered = true;
        AutoScroll = true;
        ResizeRedraw = true;
        BackColor = Color.FromArgb(245, 245, 245);

        _editMenuItem = new ToolStripMenuItem("テンプレートを編集...", null, (_, _) =>
        {
            if (_contextTarget != null) EditRequested?.Invoke(_contextTarget);
        });

        _removeMenuItem = new ToolStripMenuItem("テンプレートを削除", null, (_, _) =>
        {
            if (_contextTarget != null) RemoveRequested?.Invoke(_contextTarget);
        });

        _contextMenu.Items.AddRange([_editMenuItem, _removeMenuItem]);
    }

    public void SetProject(EditorProject? project, SpriteSheetImageCache? imageCache)
    {
        _project = project;
        _imageCache = imageCache;

        if (SelectedTemplateId.HasValue && _project?.FindEntityTemplate(SelectedTemplateId.Value) is null)
            SelectedTemplateId = null;

        RefreshList();
    }

    public void SetSelected(EntityTemplate? template)
    {
        SelectedTemplateId = template?.Id;
        Invalidate();
    }

    public void RefreshList()
    {
        RebuildLayout();
        Invalidate();
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        RebuildLayout();
    }

    private void RebuildLayout()
    {
        _items.Clear();

        var templates = _project?.EntityTemplates ?? [];
        if (templates.Count == 0)
        {
            AutoScrollMinSize = Size.Empty;
            return;
        }

        var viewportWidth = Math.Max(1, ClientSize.Width - SystemInformation.VerticalScrollBarWidth);
        var columns = Math.Max(1, viewportWidth / (ItemWidth + ItemSpacing));

        for (var i = 0; i < templates.Count; i++)
        {
            var x = ItemSpacing + (i % columns) * (ItemWidth + ItemSpacing);
            var y = ItemSpacing + (i / columns) * (ItemHeight + ItemSpacing);
            _items.Add(new TemplateLayoutItem(templates[i], new Rectangle(x, y, ItemWidth, ItemHeight)));
        }

        var rows = (templates.Count - 1) / columns + 1;
        AutoScrollMinSize = new Size(
            columns * (ItemWidth + ItemSpacing) + ItemSpacing,
            rows * (ItemHeight + ItemSpacing) + ItemSpacing);
    }

    private EntityTemplate? HitTest(Point location)
    {
        var p = new Point(location.X - AutoScrollPosition.X, location.Y - AutoScrollPosition.Y);
        return _items.FirstOrDefault(i => i.Bounds.Contains(p))?.Template;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        var g = e.Graphics;
        g.Clear(BackColor);
        g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
        g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;

        if (_items.Count == 0)
        {
            using var brush = new SolidBrush(Color.FromArgb(120, Color.Black));
            g.DrawString("No Templates\n（シートのコマを選んで「テンプレート登録」）", Font, brush,
                new RectangleF(8, 8, Math.Max(1, ClientSize.Width - 16), ClientSize.Height - 16));
            return;
        }

        foreach (var item in _items)
        {
            var rect = item.Bounds;
            rect.Offset(AutoScrollPosition);
            DrawItem(g, rect, item.Template);
        }
    }

    private void DrawItem(Graphics g, Rectangle rect, EntityTemplate template)
    {
        var isSelected = SelectedTemplateId == template.Id;

        using (var backBrush = new SolidBrush(isSelected ? Color.FromArgb(230, 245, 255) : Color.White))
            g.FillRectangle(backBrush, rect);

        using (var borderPen = new Pen(isSelected ? Color.FromArgb(220, Color.Red) : Color.FromArgb(140, Color.Gray),
                   isSelected ? 2 : 1))
            g.DrawRectangle(borderPen, rect.X, rect.Y, rect.Width - 1, rect.Height - 1);

        var thumbRect = new Rectangle(rect.X + (rect.Width - ThumbSize) / 2, rect.Y + 4, ThumbSize, ThumbSize);
        var sheet = _project?.FindSpriteSheet(template.SheetId);

        if (sheet == null || !_imageCache!.DrawTile(g, sheet, template.TileIndex, FitTile(sheet, thumbRect)))
        {
            using var missingPen = new Pen(Color.FromArgb(160, Color.OrangeRed)) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dash };
            g.DrawRectangle(missingPen, thumbRect.X, thumbRect.Y, thumbRect.Width - 1, thumbRect.Height - 1);
        }

        var labelRect = new Rectangle(rect.X + 2, thumbRect.Bottom + 2, rect.Width - 4, LabelHeight);
        TextRenderer.DrawText(g, template.Name, Font, labelRect, Color.Black,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.SingleLine);
    }

    /// <summary>コマの縦横比を保ったまま、枠内に収まる整数倍（縮小時は等倍以下）で中央配置する。</summary>
    private static Rectangle FitTile(SpriteSheet sheet, Rectangle bounds)
    {
        var scale = Math.Min((float)bounds.Width / sheet.TileWidth, (float)bounds.Height / sheet.TileHeight);
        if (scale >= 1f) scale = MathF.Floor(scale);

        var w = Math.Max(1, (int)(sheet.TileWidth * scale));
        var h = Math.Max(1, (int)(sheet.TileHeight * scale));
        return new Rectangle(bounds.X + (bounds.Width - w) / 2, bounds.Y + (bounds.Height - h) / 2, w, h);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        Focus();

        var template = HitTest(e.Location);

        if (e.Button == MouseButtons.Right)
        {
            if (template == null) return;

            _contextTarget = template;
            _contextMenu.Show(this, e.Location);
            return;
        }

        if (e.Button != MouseButtons.Left || template == null) return;

        SelectedTemplateId = template.Id;
        Invalidate();
        TemplateSelected?.Invoke(template);
    }

    protected override void OnMouseDoubleClick(MouseEventArgs e)
    {
        base.OnMouseDoubleClick(e);

        if (e.Button == MouseButtons.Left && HitTest(e.Location) is { } template)
            EditRequested?.Invoke(template);
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);

        var template = HitTest(e.Location);
        if (_hoverTemplateId == template?.Id) return;

        _hoverTemplateId = template?.Id;

        if (template == null)
        {
            _toolTip.SetToolTip(this, string.Empty);
            return;
        }

        var sheetName = _project?.FindSpriteSheet(template.SheetId)?.Name ?? "(シートなし)";
        _toolTip.SetToolTip(this,
            $"Name: {template.Name}\nType: {template.Type}\nKind: {template.Properties.Kind}\nSheet: {sheetName} #{template.TileIndex}");
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        _hoverTemplateId = null;
        _toolTip.SetToolTip(this, string.Empty);
    }

    protected override void OnScroll(ScrollEventArgs se)
    {
        base.OnScroll(se);
        Invalidate();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _toolTip.Dispose();
            _contextMenu.Dispose();
        }

        base.Dispose(disposing);
    }

    private sealed record TemplateLayoutItem(EntityTemplate Template, Rectangle Bounds);
}
