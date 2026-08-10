namespace StageSmith.Editor.Controls;

/// <summary>
/// ダブルバッファリングされたパネルコントロールです。描画のちらつきを抑えるために使用します。
/// </summary>
public class DoubleBufferedPanel : Panel
{
    public DoubleBufferedPanel()
    {
        this.DoubleBuffered = true;
        this.ResizeRedraw = true;
        this.AutoScroll = true;

        this.SetStyle(ControlStyles.AllPaintingInWmPaint |
                      ControlStyles.UserPaint |
                      ControlStyles.OptimizedDoubleBuffer,
                      true);
    }
}
