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
}
