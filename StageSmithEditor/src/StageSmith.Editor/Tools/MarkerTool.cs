using StageSmith.Core.Models;

namespace StageSmith.Editor.Tools;

/// <summary>
/// マーカー設置ツール。
/// 左クリック＝トグル設置/削除、左ドラッグ＝範囲設置（PenToolと同じ挙動）。
/// 右クリックによる明示削除は ITool 経由ではなく OnRightMouseDown を直接呼び出す形にしている
/// （ITool インターフェースには右クリックの概念がなく、MapViewControl側で個別ハンドリングするため）。
/// </summary>
public class MarkerTool : ITool
{
    private readonly MarkerState _markerState;
    private readonly Func<int> _getCurrentPageIndex;
    private readonly Action _invalidate;

    private bool _isDragging;

    public MarkerTool(
        MarkerState markerState,
        Func<int> getCurrentPageIndex,
        Action invalidate)
    {
        _markerState = markerState;
        _getCurrentPageIndex = getCurrentPageIndex;
        _invalidate = invalidate;
    }

    public void OnMouseDown(int x, int y)
    {
        _isDragging = true;

        var pageIndex = _getCurrentPageIndex();
        _markerState.Toggle(pageIndex, x, y);

        _invalidate();
    }

    public void OnMouseMove(int x, int y)
    {
        if (!_isDragging) return;

        var pageIndex = _getCurrentPageIndex();

        // ドラッグ中は「無ければ追加」のみ。
        // Toggleにすると、ドラッグの軌跡が既存マーカーの上を通過した瞬間に消えてしまうため。
        _markerState.Add(pageIndex, x, y);

        _invalidate();
    }

    public void OnMouseUp(int x, int y)
    {
        _isDragging = false;
    }

    /// <summary>
    /// 右クリックによる明示的な削除。
    /// </summary>
    public void OnRightMouseDown(int x, int y)
    {
        var pageIndex = _getCurrentPageIndex();
        _markerState.Remove(pageIndex, x, y);

        _invalidate();
    }

    public Cursor GetCursor(int x, int y)
    {
        return Cursors.Cross;
    }
}
