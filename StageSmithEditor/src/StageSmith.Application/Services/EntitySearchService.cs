using StageSmith.Core.Models;

namespace StageSmith.Application.Services;

/// <summary>
/// 配置オブジェクト検索の条件。null の項目は「絞り込まない」。
/// </summary>
/// <param name="Keyword">ID または kind の部分一致（大文字小文字は区別しない）。</param>
/// <param name="Kind">kind の完全一致。</param>
/// <param name="Palette">
/// palette の完全一致。<see cref="EntitySearchQuery.DefaultPalette"/> を指定すると palette 未指定（既定）のものに一致する。
/// </param>
/// <param name="PageIndex">指定するとそのページだけを対象にする（null ならステージ全体）。</param>
public sealed record EntitySearchQuery(
    string? Keyword = null,
    string? Kind = null,
    string? Palette = null,
    int? PageIndex = null)
{
    /// <summary>palette 未指定（既定）のものだけに絞り込むための値。</summary>
    public const string DefaultPalette = "";
}

/// <summary>検索でヒットした配置オブジェクト1件。</summary>
public sealed record EntitySearchHit(int PageIndex, Page Page, EntityPlacement Entity);

/// <summary>
/// ステージ内の配置オブジェクトを条件で絞り込む検索ロジック。
/// TileSearchService と同じく、状態を持たない純粋なクエリ処理のため static クラスとする。
/// </summary>
public static class EntitySearchService
{
    /// <summary>
    /// 条件に一致するオブジェクトを返す。並び順はページ番号昇順 → ページ内の配置順（= .def の出力順）。
    /// </summary>
    public static List<EntitySearchHit> Search(Stage stage, EntitySearchQuery query)
    {
        var hits = new List<EntitySearchHit>();

        for (var pageIndex = 0; pageIndex < stage.Pages.Count; pageIndex++)
        {
            if (query.PageIndex is { } scope && scope != pageIndex) continue;

            var page = stage.Pages[pageIndex];
            foreach (var entity in page.Entities)
            {
                if (IsMatch(entity, query))
                    hits.Add(new EntitySearchHit(pageIndex, page, entity));
            }
        }

        return hits;
    }

    private static bool IsMatch(EntityPlacement entity, EntitySearchQuery query)
    {
        var p = entity.Properties;

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            var keyword = query.Keyword.Trim();
            if (!entity.EntityId.Contains(keyword, StringComparison.OrdinalIgnoreCase) &&
                !p.Kind.Contains(keyword, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        if (query.Kind != null && p.Kind != query.Kind)
            return false;

        if (query.Palette != null && (p.Palette ?? EntitySearchQuery.DefaultPalette) != query.Palette)
            return false;

        return true;
    }
}
