namespace StageSmith.Editor.Tools;

/// <summary>
/// タイル単位ではなく部屋内ピクセル座標で入力を受け取るツール。
/// MapViewControl は現在ツールがこのインターフェースを実装している場合、
/// ITool のタイル座標メソッドの代わりにこちらを呼ぶ（Shift=フィル、Alt=スポイト等のタイル用操作も行わない）。
/// 座標は部屋の範囲外（負数や 256 以上）も渡るので、ツール側でクランプすること。
/// </summary>
public interface IPixelTool : ITool
{
    void OnPixelMouseDown(int px, int py);
    void OnPixelMouseMove(int px, int py, MouseButtons buttons);
    void OnPixelMouseUp(int px, int py);
    void OnPixelRightMouseDown(int px, int py);
    void OnPixelMouseLeave();
}
