namespace StageSmith.Core.Constants;

public static class ViewerConstants
{
    // DockPanel Suite 導入により Location/Size の直接指定は不要になった。
    // 各コントロールの配置は DockPanel が管理する。

    /// <summary>
    /// メインフォームのクライアント領域のサイズ。
    /// </summary>
    public static readonly Size MainFormClientSize = new(1200, 900);

    /// <summary>
    /// エディタ上でのタイル描画サイズ（ピクセル）。
    /// MapConstants.DefaultTileSize はゲーム側の論理サイズなので変更不可。
    /// ズーム倍率を変えたい場合はここだけ変更する。
    /// </summary>
    public const int TileRenderSize = 32; // 16 × 2倍

    /// <summary>
    /// マップビューの描画解像度（タイル数 × TileRenderSize）。
    /// </summary>
    public static readonly Size MapViewRenderSize = new(
        MapConstants.PageTileWidth * TileRenderSize,   // 16 × 32 = 512
        MapConstants.PageTileHeight * TileRenderSize    // 15 × 32 = 480
    );
}
