using System.Globalization;
using StageSmith.Core.Constants;
using StageSmith.Core.Models;

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

    //========================
    // palette（敵定義のプリセットから選ぶ／敵定義が無ければ自由入力）
    //========================

    private static readonly string DefaultPaletteLabel = $"(既定: {EntityConstants.DefaultPalette})";

    /// <summary>
    /// palette 用コンボの選択肢を、kind の敵定義にあるプリセットで作り直す。
    /// コンボは DropDown（自由入力可）で使う前提。既定（null）とプリセット名を並べる。
    /// 入力中の値は維持する。
    /// </summary>
    public static void FillPalette(ComboBox combo, EnemyDefinitionCatalog catalog, string? kind)
    {
        var current = GetEditableValue(combo);

        combo.BeginUpdate();
        combo.Items.Clear();
        combo.Items.Add(new ChoiceItem(DefaultPaletteLabel, null));

        foreach (var preset in catalog.Find(kind)?.PalettePresets ?? [])
        {
            // "default" は既定（未指定）と同じなので、重複して並べない
            if (preset.Id == EntityConstants.DefaultPalette) continue;
            combo.Items.Add(new ChoiceItem(preset.Id, preset.Id));
        }

        combo.EndUpdate();
        SetEditableValue(combo, current);
    }

    /// <summary>自由入力可のコンボから値を取り出す。既定の項目・空欄は null。</summary>
    public static string? GetEditableValue(ComboBox combo)
    {
        var text = combo.Text.Trim();
        if (text.Length == 0 || text == DefaultPaletteLabel) return null;

        return combo.SelectedItem is ChoiceItem item && item.Label == text ? item.Value : text;
    }

    public static void SetEditableValue(ComboBox combo, string? value)
    {
        for (var i = 0; i < combo.Items.Count; i++)
        {
            if (combo.Items[i] is ChoiceItem item && item.Value == value)
            {
                combo.SelectedIndex = i;
                return;
            }
        }

        combo.SelectedIndex = -1;
        combo.Text = value ?? string.Empty;
    }

    public static string? FromBool(bool? value)
        => value?.ToString(CultureInfo.InvariantCulture).ToLowerInvariant();

    public static bool? ToBool(string? value) => value switch
    {
        "true"  => true,
        "false" => false,
        _       => null,
    };
}
