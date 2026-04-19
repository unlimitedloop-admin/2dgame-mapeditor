using StageSmith.Editor.Controls;

namespace StageSmith.Editor.Tools;

public class SelectionTool : ITool
{
    private readonly MapViewControl _mapView;

    public SelectionTool(MapViewControl mapView)
    {
        _mapView = mapView;
    }

    public void OnMouseDown(int x, int y)
    {
        _mapView.BeginSelection(x, y);
    }

    public void OnMouseMove(int x, int y)
    {
        _mapView.UpdateSelection(x, y);
    }

    public void OnMouseUp(int x, int y)
    {
        _mapView.CommitSelection(x, y);
    }
}
