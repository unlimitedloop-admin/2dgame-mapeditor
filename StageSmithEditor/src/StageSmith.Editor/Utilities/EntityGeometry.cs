using StageSmith.Core.Constants;
using StageSmith.Core.Models;

namespace StageSmith.Editor.Utilities;

/// <summary>
/// エンティティ配置の座標計算（部屋内ピクセル座標系）。
/// .def の position は「足元中心」。ゲーム側はスプライト中心を (x, y - 高さ/2) に置くため、
/// スプライトの占める範囲は X: [x - 幅/2, x + 幅/2)、Y: [y - 高さ, y) になる。
/// </summary>
public static class EntityGeometry
{
    /// <summary>テンプレート・シートが見つからない場合の仮サイズ。</summary>
    public const int FallbackSize = 16;

    public static readonly IReadOnlyList<int> SnapSizes = [1, 8, 16];

    public static SpriteSheet? ResolveSheet(EditorProject? project, EntityPlacement entity)
    {
        if (project == null || entity.TemplateId is not { } templateId) return null;

        var template = project.FindEntityTemplate(templateId);
        return template == null ? null : project.FindSpriteSheet(template.SheetId);
    }

    public static Rectangle GetRoomBounds(int footX, int footY, int width, int height)
        => new(footX - width / 2, footY - height, width, height);

    public static Rectangle GetRoomBounds(EditorProject? project, EntityPlacement entity)
    {
        var sheet = ResolveSheet(project, entity);
        return GetRoomBounds(entity.X, entity.Y,
            sheet?.TileWidth ?? FallbackSize,
            sheet?.TileHeight ?? FallbackSize);
    }

    /// <summary>
    /// 足元位置をグリッドにスナップし、部屋の範囲内に収める。
    /// X はグリッド線上、Y は「グリッド線（＝タイル上端）の1px上」に合わせる。
    /// 16px スナップでタイルの上に立たせると Y = 16n - 1 になる。
    /// </summary>
    public static (int X, int Y) SnapFoot(int x, int y, int snapSize)
    {
        if (snapSize > 1)
        {
            x = (int)Math.Round(x / (double)snapSize, MidpointRounding.AwayFromZero) * snapSize;
            y = (int)Math.Round((y + 1) / (double)snapSize, MidpointRounding.AwayFromZero) * snapSize - 1;
        }

        return ClampToRoom(x, y);
    }

    public static (int X, int Y) ClampToRoom(int x, int y)
        => (Math.Clamp(x, 0, EntityConstants.RoomPixelWidth - 1),
            Math.Clamp(y, 0, EntityConstants.RoomPixelHeight - 1));
}
