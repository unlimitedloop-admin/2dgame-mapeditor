namespace StageSmith.Editor.Tools;

public class PickerTool : ITool
{
    private readonly Func<int, int, int> _pick;
    private readonly Action<int> _onPicked;

    public PickerTool(Func<int, int, int> pick, Action<int> onPicked)
    {
        _pick = pick;
        _onPicked = onPicked;
    }

    public void OnMouseDown(int x, int y)
    {
        var tile = _pick(x, y);
        _onPicked(tile);
    }

    public void OnMouseMove(int x, int y) { }
    public void OnMouseUp(int x, int y) { }
}
