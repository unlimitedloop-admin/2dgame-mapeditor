using StageSmith.Core.Models;

namespace StageSmith.Editor.Controls;

/// <summary>
/// 隣接ページの状態を表すクラスです。
/// </summary>
public sealed class MapAdjacentState
{
    public byte Up { get; init; } = 0xFF;
    public byte Down { get; init; } = 0xFF;
    public byte Left { get; init; } = 0xFF;
    public byte Right { get; init; } = 0xFF;

    public byte GetRoomId(PageDirection direction)
    {
        return direction switch
        {
            PageDirection.Up => Up,
            PageDirection.Down => Down,
            PageDirection.Left => Left,
            PageDirection.Right => Right,
            _ => 0xFF
        };
    }

    public bool HasAdjacent(PageDirection direction)
    {
        return GetRoomId(direction) != 0xFF;
    }
}
