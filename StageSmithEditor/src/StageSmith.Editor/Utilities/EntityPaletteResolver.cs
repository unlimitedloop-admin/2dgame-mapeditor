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

    public PaletteRecolor? Resolve(string? kind, string? palette)
    {
        var catalog = Catalog;
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
