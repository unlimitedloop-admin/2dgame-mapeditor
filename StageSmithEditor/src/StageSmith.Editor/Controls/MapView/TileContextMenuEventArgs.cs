namespace StageSmith.Editor.Controls;

/// <summary>
/// タイルのコンテキストメニューが要求されたときのイベント引数です。
/// </summary>
public sealed class TileContextMenuEventArgs : EventArgs
{
    public int TileX { get; }
    public int TileY { get; }
    public Point ScreenLocation { get; }

    public TileContextMenuEventArgs(int tileX, int tileY, Point screenLocation)
    {
        TileX = tileX;
        TileY = tileY;
        ScreenLocation = screenLocation;
    }
}
