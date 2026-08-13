namespace StageSmith.Core.Constants;

/// <summary>
/// マップに関する定数をまとめたクラス。
/// </summary>
public static class MapConstants
{
    /// <summary>
    /// 1ページのサイズ（バイト単位）。
    /// PageTileWidth * PageTileHeight + PageHeader.Size
    /// </summary>
    public const int PageSize = 0x100;

    /// <summary>
    /// 1ページのタイル数（横方向）。
    /// </summary>
    public const int PageTileWidth = 16;
    /// <summary>
    /// 1ページのタイル数（縦方向）。
    /// </summary>
    public const int PageTileHeight = 15;
    /// <summary>
    /// デフォルトのタイルサイズ。
    /// </summary>
    public const int DefaultTileSize = 16;
}
