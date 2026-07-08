using StageSmith.Core.Models;

namespace StageSmith.Application.Services;

/// <summary>
/// ステージ内の全ページを走査し、指定タイルIDのヒット位置一覧を返す検索ロジック。
/// 状態を持たない純粋なクエリ処理のため static クラスとする。
/// </summary>
public static class TileSearchService
{
    /// <summary>
    /// 指定タイルIDに一致するタイルを、ステージ内の全ページから走査して返す。
    /// ヒットの並び順はページ番号昇順 → 各ページ内は上から下・左から右（ラスタ順）。
    /// </summary>
    public static List<TileSearchHit> Search(Stage stage, int tileId)
    {
        var hits = new List<TileSearchHit>();

        // タイルIDは byte 範囲外なら一致しようがないため、発生源で弾く
        if (tileId < byte.MinValue || tileId > byte.MaxValue)
            return hits;

        for (var pageIndex = 0; pageIndex < stage.Pages.Count; pageIndex++)
        {
            var tileMap = stage.Pages[pageIndex].TileMap;

            for (var y = 0; y < tileMap.Height; y++)
            {
                for (var x = 0; x < tileMap.Width; x++)
                {
                    if (tileMap.GetTile(x, y) == tileId)
                    {
                        hits.Add(new TileSearchHit(pageIndex, x, y));
                    }
                }
            }
        }

        return hits;
    }
}
