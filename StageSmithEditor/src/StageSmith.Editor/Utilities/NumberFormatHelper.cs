using StageSmith.Core.Constants;

namespace StageSmith.Editor.Utilities;

/// <summary>
/// エディタ全体で使うタイル番号/座標表示の共通フォーマッタ。
/// NumberDisplayFormat設定に応じてHex/Decimal表示を切り替える。
/// </summary>
public static class NumberFormatHelper
{
    /// <summary>
    /// 汎用のバイト値表示（タイルパレットインデックス等）。
    /// Hex: 2桁ゼロ埋め・大文字（例: 0A, FF）
    /// </summary>
    public static string FormatByte(int value, NumberDisplayFormat format)
        => format == NumberDisplayFormat.Hex
            ? value.ToString("X2")
            : value.ToString();

    /// <summary>
    /// マップビューの列番号（0〜15 → Hex時 0〜F、オフセット無し）。
    /// </summary>
    public static string FormatColumnIndex(int x, NumberDisplayFormat format)
        => format == NumberDisplayFormat.Hex
            ? x.ToString("X")
            : x.ToString();

    /// <summary>
    /// マップビューの行番号。
    /// バイナリファイル上、行0はヘッダー領域が占めるため、
    /// 内部座標(0始まり)に+1した「ページ内の行番号」を表示する。
    /// Hex/Decimalどちらのモードでも+1補正は共通で適用される。
    /// 例: 内部y=0 → 表示 "1" / "01"、内部y=14 → 表示 "F" / "15"
    /// </summary>
    public static string FormatRowIndex(int y, NumberDisplayFormat format)
    {
        var displayValue = y + 1;
        return format == NumberDisplayFormat.Hex
            ? displayValue.ToString("X")
            : displayValue.ToString();
    }
}
