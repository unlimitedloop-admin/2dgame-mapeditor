namespace StageSmith.Core.Constants;

public static class ViewerConstants
{
    // DockPanel Suite 導入により Location/Size の直接指定は不要になった。
    // 各コントロールの配置は DockPanel が管理する。

    public static readonly Size MainFormClientSize = new(1200, 900);

    /// <summary>
    /// エディタ上でのタイル描画サイズ（ピクセル）。
    /// MapConstants.DefaultTileSize はゲーム側の論理サイズなので変更不可。
    /// ズーム倍率を変えたい場合はここだけ変更する。
    /// </summary>
    public const int TileRenderSize = 32; // 16 × 2倍

    /// <summary>
    /// ページナビゲーションバーの高さ（px）。
    /// 変更するとタイルパレットの高さに自動的に追従する。
    /// </summary>
    public const int NavBarHeight = 24;

    /// <summary>
    /// マップビューの描画領域マージン（px）。
    /// 上下左右に同じ幅で余白を設ける。
    /// 将来の隣接ページ移動ボタン配置のための予約領域。
    /// </summary>
    public const int MapViewMargin = 32;

    /// <summary>
    /// マップビューの描画解像度（タイル数 × TileRenderSize）。
    /// </summary>
    public static readonly Size MapViewRenderSize = new(
        MapConstants.PageTileWidth  * TileRenderSize,   // 16 × 32 = 512
        MapConstants.PageTileHeight * TileRenderSize    // 15 × 32 = 480
    );

    /// <summary>
    /// マップビューコントロール全体のサイズ（マージン込み）。
    /// SplitterDistance の計算に使用する。
    /// </summary>
    public static readonly Size MapViewContentSize = new(
        MapViewRenderSize.Width  + MapViewMargin * 2,   // 512 + 64 = 576
        MapViewRenderSize.Height + MapViewMargin * 2    // 480 + 64 = 544
    );
}