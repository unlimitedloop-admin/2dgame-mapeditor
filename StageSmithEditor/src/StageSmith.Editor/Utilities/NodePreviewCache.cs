using StageSmith.Core.Models;

namespace StageSmith.Editor.Utilities;

/// <summary>
/// ノードエディタのノード内に表示するタイルマッププレビュー（Bitmap）のキャッシュ。
///
/// NOTE: NodeEditView から抽出した。ズーム/選択/マウス操作といった
/// NodeEditView 側のUI状態には一切依存しない、Page と Bitmap(tileset) だけを
/// 入力とする純粋な生成・保持ロジックであるため、partial class ではなく
/// 独立したクラスとして切り出している（CS-001 3.3節参照）。
/// </summary>
public sealed class NodePreviewCache : IDisposable
{
    private readonly Dictionary<Guid, Bitmap> _cache = [];
    private readonly int _previewWidth;
    private readonly int _previewHeight;

    /// <summary>タイルセットの1タイルサイズ（16x16固定）。</summary>
    private const int SourceTileSize = 16;

    public NodePreviewCache(int previewWidth, int previewHeight)
    {
        _previewWidth = previewWidth;
        _previewHeight = previewHeight;
    }

    /// <summary>
    /// 指定ページIDのプレビューを取得する。無ければnull。
    /// </summary>
    public Bitmap? Get(Guid pageId)
        => _cache.TryGetValue(pageId, out var bmp) ? bmp : null;

    /// <summary>
    /// 全ページ分のプレビューを再生成する。
    /// プロジェクト読み込み時・タイルセット変更時に呼び出す。
    /// </summary>
    public void Rebuild(IReadOnlyList<Page>? pages, Bitmap? tileset)
    {
        foreach (var bmp in _cache.Values)
            bmp.Dispose();

        _cache.Clear();

        if (tileset == null || pages == null) return;

        foreach (var page in pages)
            _cache[page.Id] = Build(page, tileset);
    }

    /// <summary>
    /// 指定ページのプレビューのみ再生成する。ページ編集後に呼び出す。
    /// </summary>
    public void InvalidatePage(Guid pageId, Page page, Bitmap? tileset)
    {
        if (_cache.TryGetValue(pageId, out var old))
        {
            old.Dispose();
            _cache.Remove(pageId);
        }

        if (tileset != null)
            _cache[pageId] = Build(page, tileset);
    }

    /// <summary>
    /// 1ページ分のプレビューBitmapを生成する。
    /// タイルマップを _previewWidth x _previewHeight に縮小描画する。
    /// </summary>
    private Bitmap Build(Page page, Bitmap tileset)
    {
        var bmp = new Bitmap(_previewWidth, _previewHeight);
        using var g = Graphics.FromImage(bmp);
        g.Clear(Color.FromArgb(30, 30, 35));
        g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;

        var tileMap = page.TileMap;
        var tileW = tileMap.Width;
        var tileH = tileMap.Height;
        var cellW = (float)_previewWidth / tileW;
        var cellH = (float)_previewHeight / tileH;

        var tilesPerRow = tileset.Width / SourceTileSize;

        for (var y = 0; y < tileH; y++)
        {
            for (var x = 0; x < tileW; x++)
            {
                var tileId = tileMap.GetTile(x, y);
                if (tileId == 0) continue;

                var srcX = (tileId % tilesPerRow) * SourceTileSize;
                var srcY = (tileId / tilesPerRow) * SourceTileSize;
                var src = new Rectangle(srcX, srcY, SourceTileSize, SourceTileSize);
                var dst = new RectangleF(x * cellW, y * cellH, cellW, cellH);

                g.DrawImage(tileset, dst, src, GraphicsUnit.Pixel);
            }
        }

        return bmp;
    }

    public void Dispose()
    {
        foreach (var bmp in _cache.Values)
            bmp.Dispose();
        _cache.Clear();
    }
}
