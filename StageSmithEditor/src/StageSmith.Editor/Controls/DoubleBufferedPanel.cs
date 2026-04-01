namespace StageSmith.Editor.Controls;

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
