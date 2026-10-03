namespace StageSmith.Editor.Utilities;

/// <summary>
/// SplitContainer.SplitterDistance を安全に設定するための補助。
/// SplitterDistance の setter は値を「Panel1MinSize 〜 全長 − Panel2MinSize − SplitterWidth」に丸めるが、
/// 全長が Panel2MinSize + SplitterWidth に満たないと丸めた結果が負数になり、InvalidOperationException
/// （SplitterDistanceNotAllowed）を投げる。また Panel1MinSize を下回る位置に丸められることもある。
/// ドッキングの復元中など、ペインが一時的に小さい瞬間があるため、範囲が正しく取れるときだけ設定する。
/// </summary>
public static class SplitContainerExtensions
{
    /// <summary>
    /// 設定可能な SplitterDistance の範囲。全長が足りず範囲が空の場合は null。
    /// </summary>
    public static (int Min, int Max)? GetSplitterRange(this SplitContainer split)
    {
        var length = split.Orientation == Orientation.Horizontal ? split.Height : split.Width;

        var min = split.Panel1MinSize;
        var max = length - split.Panel2MinSize - split.SplitterWidth;

        return max >= min ? (min, max) : null;
    }

    /// <summary>
    /// value が設定可能な範囲内なら SplitterDistance に設定する。設定した（または既に同じ値だった）場合 true。
    /// </summary>
    public static bool TrySetSplitterDistance(this SplitContainer split, int value)
    {
        if (split.GetSplitterRange() is not { } range || value < range.Min || value > range.Max)
            return false;

        if (split.SplitterDistance != value)
            split.SplitterDistance = value;

        return true;
    }

    /// <summary>
    /// 全長に対する比率で SplitterDistance を設定する（範囲に収まるよう丸める）。範囲が空なら何もせず false。
    /// </summary>
    public static bool TrySetSplitterRatio(this SplitContainer split, double ratio)
    {
        if (split.GetSplitterRange() is not { } range) return false;

        var length = split.Orientation == Orientation.Horizontal ? split.Height : split.Width;
        return split.TrySetSplitterDistance(Math.Clamp((int)(length * ratio), range.Min, range.Max));
    }
}
