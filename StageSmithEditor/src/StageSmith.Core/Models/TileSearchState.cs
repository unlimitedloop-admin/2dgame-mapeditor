namespace StageSmith.Core.Models;

/// <summary>
/// タイル検索のヒット1件（ページ・座標）を表す。
/// </summary>
public readonly record struct TileSearchHit(int PageIndex, int X, int Y);

/// <summary>
/// タイル検索の状態（検索対象タイルID、ヒット一覧、現在位置、検索オプション）を管理する。
/// </summary>
public sealed class TileSearchState
{
    private readonly List<TileSearchHit> _hits = [];

    public int TargetTileId { get; private set; } = -1;

    public int CurrentIndex { get; private set; } = -1;

    public IReadOnlyList<TileSearchHit> Hits => _hits;

    public bool HasHits => _hits.Count > 0;

    // ===== 検索オプション =====
    public bool WrapAround { get; set; } = true;

    public bool ShowHighlight { get; set; } = true;

    public TileSearchHit? CurrentHit =>
        CurrentIndex >= 0 && CurrentIndex < _hits.Count
            ? _hits[CurrentIndex]
            : null;

    // ===== 置換オプション =====
    public int ReplaceTileId { get; set; } = -1;

    /// <summary>true の場合、検索・ナビゲーション・全て置換のすべてを現在ページのみに限定する。</summary>
    public bool ScopeCurrentPageOnly { get; set; } = false;

    public event Action? Changed;

    private void NotifyChanged()
    {
        Changed?.Invoke();
    }

    /// <summary>
    /// 検索結果をセットし、先頭ヒットへ位置付ける。
    /// </summary>
    public void SetHits(int targetTileId, IEnumerable<TileSearchHit> hits)
    {
        TargetTileId = targetTileId;
        _hits.Clear();
        _hits.AddRange(hits);
        CurrentIndex = _hits.Count > 0 ? 0 : -1;

        NotifyChanged();
    }

    /// <summary>
    /// 検索状態をクリアする（ハイライト解除時にも使用）。
    /// </summary>
    public void Clear()
    {
        TargetTileId = -1;
        _hits.Clear();
        CurrentIndex = -1;

        NotifyChanged();
    }

    /// <summary>
    /// 次のヒットへ進む。末尾に達した場合はWrapAroundに従う。
    /// </summary>
    public bool MoveNext()
    {
        if (_hits.Count == 0) return false;

        if (CurrentIndex + 1 < _hits.Count)
        {
            CurrentIndex++;
        }
        else if (WrapAround)
        {
            CurrentIndex = 0;
        }
        else
        {
            return false;
        }

        NotifyChanged();
        return true;
    }

    /// <summary>
    /// 前のヒットへ戻る。先頭に達した場合はWrapAroundに従う。
    /// </summary>
    public bool MovePrevious()
    {
        if (_hits.Count == 0) return false;

        if (CurrentIndex - 1 >= 0)
        {
            CurrentIndex--;
        }
        else if (WrapAround)
        {
            CurrentIndex = _hits.Count - 1;
        }
        else
        {
            return false;
        }

        NotifyChanged();
        return true;
    }

    /// <summary>
    /// 指定ページに属するヒットのみを取得する（MapViewControlのハイライト描画用）。
    /// </summary>
    public IEnumerable<TileSearchHit> GetHitsForPage(int pageIndex)
    {
        return _hits.Where(h => h.PageIndex == pageIndex);
    }
}
