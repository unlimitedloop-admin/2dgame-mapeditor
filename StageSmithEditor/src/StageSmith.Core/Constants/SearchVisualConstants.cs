namespace StageSmith.Editor.Constants;

/// <summary>
/// タイル検索結果のハイライト表示に関する色定義。
/// ここを変更するだけで検索ハイライトの見た目を調整できる。
/// </summary>
public static class SearchVisualConstants
{
    /// <summary>
    /// 検索結果ハイライト（塗り）の色。
    /// </summary>
    public static readonly Color HighlightFillColor = Color.FromArgb(100, 255, 255, 0);

    /// <summary>
    /// 現在のジャンプ先タイルに表示する枠線の色（ハイライトON/OFFどちらでも表示される）。
    /// </summary>
    public static readonly Color HighlightBorderColor = Color.FromArgb(220, 255, 165, 0);

    public const int HighlightBorderWidth = 2;
}
