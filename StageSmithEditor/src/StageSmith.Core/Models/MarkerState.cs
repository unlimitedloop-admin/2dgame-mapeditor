namespace StageSmith.Core.Models;

/// <summary>
/// マーカーの状態（全ページ分の一覧、現在位置）を管理する。
/// ステージ単位で保持し、ページをまたいで巡回できるようにする。
/// 保存対象外（エディタ実行中のみの作業メモ）。
/// </summary>
public sealed class MarkerState
{
    private readonly List<Marker> _markers = [];

    public int CurrentIndex { get; private set; } = -1;

    public IReadOnlyList<Marker> Markers => _markers;

    public bool HasMarkers => _markers.Count > 0;

    /// <summary>Next/Previousで末尾/先頭から折り返すかどうか。タイル検索と挙動を揃えて既定 true。</summary>
    public bool WrapAround { get; set; } = true;

    public bool ShowOverlay { get; set; } = false;

    public Marker? CurrentMarker =>
        CurrentIndex >= 0 && CurrentIndex < _markers.Count
            ? _markers[CurrentIndex]
            : null;

    public event Action? Changed;

    private void NotifyChanged()
    {
        Changed?.Invoke();
    }

    /// <summary>
    /// 指定位置にマーカーがあれば削除、なければ追加する（トグル）。
    /// 追加した場合はそのマーカーを現在位置にする。
    /// </summary>
    /// <returns>追加した場合 true、削除した場合 false。</returns>
    public bool Toggle(Guid stageId, int pageIndex, int x, int y)
    {
        var index = _markers.FindIndex(m => m.StageId == stageId && m.PageIndex == pageIndex && m.X == x && m.Y == y);

        if (index >= 0)
        {
            _markers.RemoveAt(index);

            if (CurrentIndex >= _markers.Count)
                CurrentIndex = _markers.Count - 1;

            NotifyChanged();
            return false;
        }

        _markers.Add(new Marker(stageId, pageIndex, x, y));
        CurrentIndex = _markers.Count - 1;

        NotifyChanged();
        return true;
    }

    /// <summary>
    /// 指定位置のマーカーを明示的に削除する（右クリック用）。既に無ければ何もしない。
    /// </summary>
    public void Remove(Guid stageId, int pageIndex, int x, int y)
    {
        var index = _markers.FindIndex(m => m.StageId == stageId && m.PageIndex == pageIndex && m.X == x && m.Y == y);
        if (index < 0) return;

        _markers.RemoveAt(index);

        if (CurrentIndex >= _markers.Count)
            CurrentIndex = _markers.Count - 1;

        NotifyChanged();
    }

    /// <summary>
    /// 指定位置にマーカーが既にあれば追加せず false を返す（範囲設置での重複防止用）。
    /// </summary>
    public bool Add(Guid stageId, int pageIndex, int x, int y)
    {
        if (_markers.Any(m => m.StageId == stageId && m.PageIndex == pageIndex && m.X == x && m.Y == y))
            return false;

        _markers.Add(new Marker(stageId, pageIndex, x, y));
        CurrentIndex = _markers.Count - 1;

        NotifyChanged();
        return true;
    }

    /// <summary>
    /// プロジェクト全体の全マーカーを削除する。
    /// </summary>
    public void Clear()
    {
        if (_markers.Count == 0) return;

        _markers.Clear();
        CurrentIndex = -1;

        NotifyChanged();
    }

    /// <summary>
    /// 指定ステージ・ページに属するマーカーのみを削除する（ページ単位削除）。
    /// </summary>
    public void ClearForPage(Guid stageId, int pageIndex)
    {
        var removed = _markers.RemoveAll(m => m.StageId == stageId && m.PageIndex == pageIndex);
        if (removed == 0) return;

        if (CurrentIndex >= _markers.Count)
            CurrentIndex = _markers.Count - 1;

        NotifyChanged();
    }

    /// <summary>
    /// 指定ステージに属するマーカーのみを削除する（ステージ単位削除）。
    /// </summary>
    public void ClearForStage(Guid stageId)
    {
        var removed = _markers.RemoveAll(m => m.StageId == stageId);
        if (removed == 0) return;

        if (CurrentIndex >= _markers.Count)
            CurrentIndex = _markers.Count - 1;

        NotifyChanged();
    }

    public bool MoveNext()
    {
        if (_markers.Count == 0) return false;

        if (CurrentIndex + 1 < _markers.Count)
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

    public bool MovePrevious()
    {
        if (_markers.Count == 0) return false;

        if (CurrentIndex - 1 >= 0)
        {
            CurrentIndex--;
        }
        else if (WrapAround)
        {
            CurrentIndex = _markers.Count - 1;
        }
        else
        {
            return false;
        }

        NotifyChanged();
        return true;
    }

    /// <summary>
    /// 指定ステージの指定ページに属するマーカーのみを取得する（MapViewControlのオーバーレイ描画用）。
    /// </summary>
    public IEnumerable<Marker> GetMarkersForPage(Guid stageId, int pageIndex)
    {
        return _markers.Where(m => m.StageId == stageId && m.PageIndex == pageIndex);
    }
}
