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

    /// <summary>プレイヤー開始位置マーカーの大きさ（部屋内px）。見た目の目安で、ゲーム側の判定とは無関係。</summary>
    public const int PlayerStartMarkerWidth = 16;
    public const int PlayerStartMarkerHeight = 24;

    public static Rectangle GetPlayerStartBounds(int footX, int footY)
        => GetRoomBounds(footX, footY, PlayerStartMarkerWidth, PlayerStartMarkerHeight);

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
    /// 着地時にスプライト下端が床タイル上端より沈む量（px）。
    /// ゲーム側 EnemyEntity の kVisualFloorSinkPx と一致させること。
    /// キャラクターの最下行が床の最上行に1px重なる接触判定仕様による。
    /// </summary>
    public const int FloorSinkPx = 1;

    /// <summary>
    /// 足元位置をグリッドにスナップし、部屋の範囲内に収める。
    /// .def の (x, y) はピクセルの境界座標（x=スプライト左右の境目、y=スプライト下端）。
    /// X はグリッド線上、Y は「グリッド線（＝床タイル上端）＋FloorSinkPx」に合わせる。
    /// 16px スナップで床に立たせると Y = 16n + 1 となり、ゲーム内で着地した状態と一致する。
    /// </summary>
    public static (int X, int Y) SnapFoot(int x, int y, int snapSize)
    {
        if (snapSize > 1)
        {
            x = (int)Math.Round(x / (double)snapSize, MidpointRounding.AwayFromZero) * snapSize;
            y = (int)Math.Round((y - FloorSinkPx) / (double)snapSize, MidpointRounding.AwayFromZero) * snapSize + FloorSinkPx;
        }

        return ClampToRoom(x, y);
    }

    public static (int X, int Y) ClampToRoom(int x, int y)
        => (Math.Clamp(x, 0, EntityConstants.RoomPixelWidth - 1),
            Math.Clamp(y, 0, EntityConstants.RoomPixelHeight - 1));
}
