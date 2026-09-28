namespace StageSmith.Core.Constants;

/// <summary>
/// エンティティ配置（.def の entities[] / stage.start / nodes[].enemyRespawn）に関する定数。
/// 文字列値はゲーム側 StageDefinitionLoader が受け付ける値と完全一致させること。
/// </summary>
public static class EntityConstants
{
    /// <summary>エンティティ種別。ゲーム側が現在読むのは Enemy のみ。</summary>
    public const string TypeEnemy = "enemy";

    /// <summary>properties.palette 未指定時にゲーム側が使うパレット名。</summary>
    public const string DefaultPalette = "default";

    // --- properties.facing ---
    public const string FacingPlayer = "player";
    public const string FacingLeft   = "left";
    public const string FacingRight  = "right";

    public static readonly IReadOnlyList<string> FacingValues =
        [FacingPlayer, FacingLeft, FacingRight];

    // --- properties.respawn / nodes[].enemyRespawn ---
    public const string RespawnAlways        = "always";
    public const string RespawnUntilDefeated = "until_defeated";
    public const string RespawnOnce          = "once";

    public static readonly IReadOnlyList<string> RespawnValues =
        [RespawnAlways, RespawnUntilDefeated, RespawnOnce];

    /// <summary>
    /// 部屋内ピクセル座標の幅（X は 0 〜 RoomPixelWidth-1）。
    /// ゲーム側 SystemConfig::kScreenWidth と一致する。
    /// </summary>
    public const int RoomPixelWidth = MapConstants.PageTileWidth * MapConstants.DefaultTileSize;

    /// <summary>
    /// 部屋内ピクセル座標の高さ（Y は 0 〜 RoomPixelHeight-1）。
    /// ゲーム側 SystemConfig::kScreenHeight と一致する。
    /// </summary>
    public const int RoomPixelHeight = MapConstants.PageTileHeight * MapConstants.DefaultTileSize;

    /// <summary>RoomId 未割当を表す値。この部屋に置いたエンティティは .def に出力できない。</summary>
    public const byte UnassignedRoomId = 0xFF;

    public static bool IsInsideRoom(int x, int y)
        => x >= 0 && x < RoomPixelWidth && y >= 0 && y < RoomPixelHeight;
}
