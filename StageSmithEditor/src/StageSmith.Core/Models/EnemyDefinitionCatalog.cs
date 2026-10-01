namespace StageSmith.Core.Models;

/// <summary>NES パレットの1色。</summary>
public readonly record struct NesColor(byte R, byte G, byte B);

/// <summary>
/// NES パレット（色番号 0〜63 → RGB）。ゲーム側 assets/system/nes_palette.txt を読み込んだもの。
/// </summary>
public sealed class NesPalette
{
    public const int ColorCount = 64;

    private readonly NesColor?[] _colors = new NesColor?[ColorCount];

    public void Set(int index, NesColor color)
    {
        if (index is >= 0 and < ColorCount)
            _colors[index] = color;
    }

    /// <summary>色番号に対応する色。未定義の番号なら null。</summary>
    public NesColor? Get(int index)
        => index is >= 0 and < ColorCount ? _colors[index] : null;
}

/// <summary>敵定義の palette_presets[].mappings[] の1件（NES 色番号の置き換え）。</summary>
public readonly record struct EnemyPaletteMapping(int Source, int Target);

/// <summary>敵定義の palette_presets[] の1件。</summary>
public sealed class EnemyPalettePreset
{
    public string Id { get; init; } = string.Empty;
    public List<EnemyPaletteMapping> Mappings { get; init; } = [];
}

/// <summary>
/// エディタが参照する、敵定義JSON（例: METALL.json）の必要部分。
/// ゲーム側が正本なので、エディタは読むだけで保存しない。
/// </summary>
public sealed class EnemyDefinitionInfo
{
    /// <summary>敵定義の id（.def の properties.kind に書く値）。</summary>
    public string Id { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public string FilePath { get; init; } = string.Empty;

    /// <summary>abilities.sprite_half_size。未定義なら null。</summary>
    public (double X, double Y)? SpriteHalfSize { get; init; }

    public List<EnemyPalettePreset> PalettePresets { get; init; } = [];

    public EnemyPalettePreset? FindPreset(string id)
        => PalettePresets.FirstOrDefault(p => p.Id == id);
}

/// <summary>
/// 敵定義フォルダと NES パレットを読み込んだ結果（実行時のみ・保存しない）。
/// </summary>
public sealed class EnemyDefinitionCatalog
{
    /// <summary>palette 未指定時にゲーム側が使うプリセット名。</summary>
    public const string DefaultPaletteId = "default";

    /// <summary>
    /// 敵定義が無い状態のカタログ。呼び出し側での書き換え（TryAdd・Errors・NesPalette）が
    /// 他の利用箇所へ波及しないよう、共有インスタンスではなく毎回新しく作って返す。
    /// </summary>
    public static EnemyDefinitionCatalog Empty => new();

    private readonly Dictionary<string, EnemyDefinitionInfo> _definitions = new(StringComparer.Ordinal);

    public NesPalette? NesPalette { get; set; }

    /// <summary>読み込み時の問題（壊れたJSON、id 重複など）。致命的ではないものも含む。</summary>
    public List<string> Errors { get; } = [];

    /// <summary>敵定義が1件以上読み込めているか。false の間は kind / palette の検証を行わない。</summary>
    public bool HasDefinitions => _definitions.Count > 0;

    public IEnumerable<EnemyDefinitionInfo> Definitions => _definitions.Values.OrderBy(d => d.Id, StringComparer.Ordinal);

    /// <summary>id が重複していた場合は false（先に読んだ方を残す）。</summary>
    public bool TryAdd(EnemyDefinitionInfo definition) => _definitions.TryAdd(definition.Id, definition);

    public EnemyDefinitionInfo? Find(string? kind)
        => kind != null && _definitions.TryGetValue(kind, out var definition) ? definition : null;

    /// <summary>
    /// ゲーム側 EnemySpawnDirector と同じ規則で、実際に使われるプリセットを決める。
    /// palette 未指定は "default"、プリセットに無い palette は先頭のプリセットにフォールバックする。
    /// </summary>
    public EnemyPalettePreset? ResolvePreset(string? kind, string? palette)
    {
        var definition = Find(kind);
        if (definition == null || definition.PalettePresets.Count == 0) return null;

        return definition.FindPreset(palette ?? DefaultPaletteId) ?? definition.PalettePresets[0];
    }

    /// <summary>
    /// palette がその kind のプリセットとして定義されているか。
    /// kind が未知、またはプリセットが1件も無い敵定義の場合は判定できないので null。
    /// </summary>
    public bool? IsKnownPalette(string? kind, string? palette)
    {
        var definition = Find(kind);
        if (definition == null || definition.PalettePresets.Count == 0) return null;

        return definition.FindPreset(palette ?? DefaultPaletteId) != null;
    }
}
