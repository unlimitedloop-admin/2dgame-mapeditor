using System.Globalization;
using StageSmith.Core.Constants;

namespace StageSmith.Editor.Utilities;

/// <summary>
/// 「未指定(null)」を含む文字列選択肢をコンボボックスに載せるための項目。
/// エンティティの facing / respawn / despawnOffscreen など、.def で省略可能な値の編集に使う。
/// </summary>
public sealed record ChoiceItem(string Label, string? Value)
{
    public override string ToString() => Label;

    public static void Fill(ComboBox combo, string nullLabel, IEnumerable<string> values)
    {
        combo.Items.Clear();
        combo.Items.Add(new ChoiceItem(nullLabel, null));
        foreach (var v in values)
            combo.Items.Add(new ChoiceItem(v, v));
    }

    public static void FillFacing(ComboBox combo)
        => Fill(combo, $"(既定: {EntityConstants.FacingPlayer})", EntityConstants.FacingValues);

    public static void FillRespawn(ComboBox combo, string nullLabel = "(部屋の既定に従う)")
        => Fill(combo, nullLabel, EntityConstants.RespawnValues);

    public static void FillDespawn(ComboBox combo)
        => Fill(combo, "(既定: true)", ["true", "false"]);

    public static void Select(ComboBox combo, string? value)
    {
        for (var i = 0; i < combo.Items.Count; i++)
        {
            if (combo.Items[i] is ChoiceItem item && item.Value == value)
            {
                combo.SelectedIndex = i;
                return;
            }
        }

        combo.SelectedIndex = combo.Items.Count > 0 ? 0 : -1;
    }

    public static string? GetValue(ComboBox combo) => (combo.SelectedItem as ChoiceItem)?.Value;

    public static string? FromBool(bool? value)
        => value?.ToString(CultureInfo.InvariantCulture).ToLowerInvariant();

    public static bool? ToBool(string? value) => value switch
    {
        "true"  => true,
        "false" => false,
        _       => null,
    };
}
