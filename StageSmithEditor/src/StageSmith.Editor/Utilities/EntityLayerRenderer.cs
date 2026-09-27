using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using StageSmith.Core.Constants;
using StageSmith.Core.Models;
using StageSmith.Editor.Tools;

namespace StageSmith.Editor.Utilities;

/// <summary>
/// マップビュー上にエンティティ（敵など）を描画するレイヤー。
/// 配置済みエンティティは常に描画し、オブジェクトツール使用中のみ
/// 選択枠・足元マーカー・配置プレビューを重ねる。
/// 座標は部屋内ピクセル × scale（画面px / 部屋px）＋ オフセットで画面座標へ変換する。
/// </summary>
public sealed class EntityLayerRenderer
{
    private const float PreviewAlpha = 0.5f;

    private static readonly Color SelectedColor = Color.Red;
    private static readonly Color FootMarkerColor = Color.Yellow;
    private static readonly Color MissingColor = Color.OrangeRed;

    private readonly SpriteSheetImageCache _imageCache;
    private readonly Func<EditorProject?> _getProject;
    private readonly Func<Page?> _getPage;
    private readonly Func<EntityTemplate?> _getTemplate;
    private readonly ObjectTool _tool;
    private readonly Func<bool> _isToolActive;

    public EntityLayerRenderer(
        SpriteSheetImageCache imageCache,
        Func<EditorProject?> getProject,
        Func<Page?> getPage,
        Func<EntityTemplate?> getTemplate,
        ObjectTool tool,
        Func<bool> isToolActive)
    {
        _imageCache = imageCache;
        _getProject = getProject;
        _getPage = getPage;
        _getTemplate = getTemplate;
        _tool = tool;
        _isToolActive = isToolActive;
    }

    public void Draw(Graphics g, int offsetX, int offsetY, float scale)
    {
        var page = _getPage();
        if (page == null) return;

        var project = _getProject();
        var toolActive = _isToolActive();

        foreach (var entity in page.Entities)
        {
            var sheet = EntityGeometry.ResolveSheet(project, entity);
            var template = entity.TemplateId is { } id ? project?.FindEntityTemplate(id) : null;
            var dst = ToScreen(EntityGeometry.GetRoomBounds(project, entity), offsetX, offsetY, scale);

            if (template == null || !_imageCache.DrawTile(g, sheet, template.TileIndex, dst))
                DrawMissing(g, dst, entity.Properties.Kind);

            if (toolActive)
                DrawFootMarker(g, entity.X, entity.Y, offsetX, offsetY, scale);
        }

        if (!toolActive) return;

        if (_tool.SelectedEntity is { } selected)
            DrawSelection(g, project, selected, offsetX, offsetY, scale);

        DrawPreview(g, offsetX, offsetY, scale);
    }

    private void DrawSelection(Graphics g, EditorProject? project, EntityPlacement entity, int offsetX, int offsetY, float scale)
    {
        var rect = ToScreen(EntityGeometry.GetRoomBounds(project, entity), offsetX, offsetY, scale);

        using var pen = new Pen(SelectedColor, 2);
        g.DrawRectangle(pen, rect.X, rect.Y, rect.Width - 1, rect.Height - 1);

        // 枠の上に .def の id と座標を出す（上にはみ出す場合は枠の下へ）
        var label = $"{entity.EntityId} ({entity.X}, {entity.Y})";
        using var font = new Font("Yu Gothic UI", 8f);
        var size = g.MeasureString(label, font);
        var labelY = rect.Y - size.Height - 1 >= offsetY ? rect.Y - size.Height - 1 : rect.Bottom + 1;

        // 部屋の右端に近い場合でも見切れないよう、部屋の内側へ寄せる
        var roomRight = offsetX + EntityConstants.RoomPixelWidth * scale;
        var labelX = Math.Max(offsetX, Math.Min(rect.X, roomRight - size.Width));

        using var back = new SolidBrush(Color.FromArgb(180, Color.Black));
        using var fore = new SolidBrush(Color.White);
        g.FillRectangle(back, labelX, labelY, size.Width, size.Height);
        g.DrawString(label, font, fore, labelX, labelY);
    }

    private void DrawPreview(Graphics g, int offsetX, int offsetY, float scale)
    {
        if (_tool.PreviewFoot is not { } foot) return;

        var template = _getTemplate();
        if (template == null) return;

        var sheet = _getProject()?.FindSpriteSheet(template.SheetId);
        var room = EntityGeometry.GetRoomBounds(foot.X, foot.Y,
            sheet?.TileWidth ?? EntityGeometry.FallbackSize,
            sheet?.TileHeight ?? EntityGeometry.FallbackSize);
        var dst = ToScreen(room, offsetX, offsetY, scale);

        using var attributes = new ImageAttributes();
        attributes.SetColorMatrix(new ColorMatrix { Matrix33 = PreviewAlpha });

        if (!_imageCache.DrawTile(g, sheet, template.TileIndex, dst, attributes))
            DrawMissing(g, dst, template.Properties.Kind);

        DrawFootMarker(g, foot.X, foot.Y, offsetX, offsetY, scale);
    }

    private static void DrawFootMarker(Graphics g, int footX, int footY, int offsetX, int offsetY, float scale)
    {
        // 足元の1px（部屋座標）の中心に十字を描く
        var cx = offsetX + (footX + 0.5f) * scale;
        var cy = offsetY + (footY + 0.5f) * scale;
        const float arm = 4f;

        using var shadow = new Pen(Color.FromArgb(160, Color.Black), 3);
        using var pen = new Pen(FootMarkerColor, 1);

        g.DrawLine(shadow, cx - arm, cy, cx + arm, cy);
        g.DrawLine(shadow, cx, cy - arm, cx, cy + arm);
        g.DrawLine(pen, cx - arm, cy, cx + arm, cy);
        g.DrawLine(pen, cx, cy - arm, cx, cy + arm);
    }

    private static void DrawMissing(Graphics g, Rectangle dst, string kind)
    {
        using var fill = new SolidBrush(Color.FromArgb(60, MissingColor));
        using var pen = new Pen(MissingColor) { DashStyle = DashStyle.Dash };
        g.FillRectangle(fill, dst);
        g.DrawRectangle(pen, dst.X, dst.Y, dst.Width - 1, dst.Height - 1);

        if (string.IsNullOrEmpty(kind)) return;

        using var font = new Font("Yu Gothic UI", 7f);
        using var brush = new SolidBrush(Color.White);
        g.DrawString(kind, font, brush, new RectangleF(dst.X + 1, dst.Y + 1, Math.Max(1, dst.Width - 2), Math.Max(1, dst.Height - 2)));
    }

    private static Rectangle ToScreen(Rectangle room, int offsetX, int offsetY, float scale)
    {
        return Rectangle.FromLTRB(
            offsetX + (int)MathF.Round(room.Left * scale),
            offsetY + (int)MathF.Round(room.Top * scale),
            offsetX + (int)MathF.Round(room.Right * scale),
            offsetY + (int)MathF.Round(room.Bottom * scale));
    }
}
