using StageSmith.Core.Models;

namespace StageSmith.Editor.Utilities;

/// <summary>
/// エンティティの kind / palette から、描画用の色替え（PaletteRecolor）を求める。
/// 使うプリセットの決め方はゲーム側と同じ（未指定は "default"、未知の palette は先頭のプリセット）。
/// 敵定義または NES パレットが読み込まれていない場合は色替えしない（元画像のまま表示）。
/// </summary>
public sealed class EntityPaletteResolver
{
    private readonly Func<EnemyDefinitionCatalog?> _getCatalog;

    public EntityPaletteResolver(Func<EnemyDefinitionCatalog?> getCatalog)
    {
        _getCatalog = getCatalog;
    }

    public EnemyDefinitionCatalog Catalog => _getCatalog() ?? EnemyDefinitionCatalog.Empty;

    public PaletteRecolor? Resolve(EntityProperties properties)
        => Resolve(properties.Kind, properties.Palette);

    // 描画のたびに呼ばれるため、(kind, palette) ごとの結果をキャッシュする。
    // 敵定義を読み直すとカタログのインスタンスが変わるので、そのときにキャッシュを捨てる。
    private readonly Dictionary<(string? Kind, string? Palette), PaletteRecolor?> _cache = [];
    private EnemyDefinitionCatalog? _cachedCatalog;

    public PaletteRecolor? Resolve(string? kind, string? palette)
    {
        var catalog = Catalog;

        if (!ReferenceEquals(catalog, _cachedCatalog))
        {
            _cache.Clear();
            _cachedCatalog = catalog;
        }

        if (_cache.TryGetValue((kind, palette), out var cached))
            return cached;

        var resolved = Build(catalog, kind, palette);
        _cache[(kind, palette)] = resolved;
        return resolved;
    }

    private static PaletteRecolor? Build(EnemyDefinitionCatalog catalog, string? kind, string? palette)
    {
        var nes = catalog.NesPalette;
        if (nes == null) return null;

        var preset = catalog.ResolvePreset(kind, palette);
        if (preset == null || preset.Mappings.Count == 0) return null;

        var mappings = new List<(Color From, Color To)>(preset.Mappings.Count);
        foreach (var mapping in preset.Mappings)
        {
            if (nes.Get(mapping.Source) is not { } from || nes.Get(mapping.Target) is not { } to)
                continue;

            mappings.Add((Color.FromArgb(from.R, from.G, from.B), Color.FromArgb(to.R, to.G, to.B)));
        }

        return mappings.Count == 0 ? null : new PaletteRecolor($"{kind}|{preset.Id}", mappings);
    }
}
