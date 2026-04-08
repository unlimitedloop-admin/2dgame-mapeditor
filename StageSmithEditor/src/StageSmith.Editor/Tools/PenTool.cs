namespace StageSmith.Editor.Tools;

public class PenTool : ITool
{
    private readonly Action<int, int> _paint;

    public PenTool(Action<int, int> paint)
    {
        _paint = paint;
    }

    public void OnMouseDown(int x, int y) => _paint(x, y);
    public void OnMouseMove(int x, int y) => _paint(x, y);
    public void OnMouseUp(int x, int y) { }
}
