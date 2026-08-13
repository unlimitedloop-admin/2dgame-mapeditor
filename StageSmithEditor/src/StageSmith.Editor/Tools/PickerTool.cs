using StageSmith.Editor.Utilities;

namespace StageSmith.Editor.Tools;

/// <summary>
/// ピッカーツール。
/// 選択された座標のタイルIDを取得し、指定されたコールバックに渡す。
/// </summary>
public class PickerTool : ITool
{
    private readonly Func<int, int, int> _pick;
    private readonly Action<int> _onPicked;

    private static Cursor? _pickerCursor;

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

    public Cursor GetCursor(int x, int y)
    {
        _pickerCursor ??= CursorFactory.FromPng(@"resource/cur/icons8-色スポイト-30.png", 3, 25);
        
        return _pickerCursor;
    }
}
