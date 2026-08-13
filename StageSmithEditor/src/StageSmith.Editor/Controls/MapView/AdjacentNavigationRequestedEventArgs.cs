using StageSmith.Core.Models;

namespace StageSmith.Editor.Controls;

/// <summary>
/// 隣接ページナビゲーションが要求されたときのイベント引数です。
/// </summary>
public sealed class AdjacentNavigationRequestedEventArgs : EventArgs
{
    public PageDirection Direction { get; }
    public bool HasAdjacentPage { get; }

    public AdjacentNavigationRequestedEventArgs(PageDirection direction, bool hasAdjacentPage)
    {
        Direction = direction;
        HasAdjacentPage = hasAdjacentPage;
    }
}
