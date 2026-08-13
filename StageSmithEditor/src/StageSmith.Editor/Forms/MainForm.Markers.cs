using StageSmith.Editor.DockContents;

namespace StageSmith.Editor;

public partial class MainForm
{
    private void BindMarkerColorPanel()
    {
        _markerColorPanel.SetColor(_mapView.MarkerColor);
        _markerColorPanel.ColorChanged += color => _mapView.SetMarkerColor(color);
    }

    private void MoveToNextMarker()
    {
        if (!_markerState.MoveNext()) return;
        JumpToCurrentMarkerIfNeeded();
    }

    private void MoveToPreviousMarker()
    {
        if (!_markerState.MovePrevious()) return;
        JumpToCurrentMarkerIfNeeded();
    }

    private void JumpToCurrentMarkerIfNeeded()
    {
        if (_markerState.CurrentMarker is not { } marker) return;

        if (_context.CurrentPageIndex != marker.PageIndex)
        {
            _context.SetPage(marker.PageIndex);
        }

        _mapViewContent.ScrollToTile(marker.X, marker.Y);
    }

    /// <summary>
    /// 現在のステージ・現在のページに属するマーカーのみを削除する。
    /// </summary>
    private void ClearPageMarkers()
    {
        if (_context.CurrentStage is not { } stage) return;

        _markerState.ClearForPage(stage.Id, _context.CurrentPageIndex);
    }

    /// <summary>
    /// 現在のステージに属するマーカーをすべて削除する。
    /// </summary>
    private void ClearStageMarkers()
    {
        if (_context.CurrentStage is not { } stage) return;

        _markerState.ClearForStage(stage.Id);
    }

    /// <summary>
    /// プロジェクト全体のマーカーをすべて削除する。
    /// </summary>
    private void ClearAllMarkers()
    {
        _markerState.Clear();
    }
}
