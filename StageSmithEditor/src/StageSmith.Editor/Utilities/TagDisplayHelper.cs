using StageSmith.Core.Models;

namespace StageSmith.Editor.Utilities;

/// <summary>
/// 複数箇所（Property Window, Stage Explorer等）で共通して使う、
/// タグの代表選出・視認性補正ロジック。
/// </summary>
public static class TagDisplayHelper
{
    /// <summary>
    /// 付与されているタグの中から「代表タグ」を1つ選ぶ。
    /// Priority値が最小（＝優先度が高い）ものを優先し、同値ならLabel昇順。
    /// </summary>
    public static Tag? ResolveRepresentativeTag(EditorProject? project, List<string>? tagIds)
    {
        if (project == null || tagIds == null || tagIds.Count == 0)
            return null;

        return tagIds
            .Select(idStr => Guid.TryParse(idStr, out var id) ? project.FindTag(id) : null)
            .Where(t => t != null)
            .Select(t => t!)
            .OrderBy(t => t.Priority)
            .ThenBy(t => t.Label)
            .FirstOrDefault();
    }

    /// <summary>
    /// 白背景上で視認できるよう、明るすぎる色を暗く補正する。
    /// 単純なRGB縮小のため色相が微妙にズレる場合があるが、実用上は十分。  
    /// </summary>
    public static Color GetReadableTextColor(Color source)
    {
        var luminance = source.R * 0.299 + source.G * 0.587 + source.B * 0.114;
        if (luminance < 140)
            return source; // 十分暗ければそのまま使う

        const double darkenFactor = 0.55;
        return Color.FromArgb(
            (int)(source.R * darkenFactor),
            (int)(source.G * darkenFactor),
            (int)(source.B * darkenFactor));
    }
}
