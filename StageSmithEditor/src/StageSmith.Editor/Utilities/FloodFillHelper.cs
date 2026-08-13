using StageSmith.Core.Models;

namespace StageSmith.Editor.Utilities;

/// <summary>
/// 指定したタイルマップの中で、指定座標から同じタイルIDの領域を探索するFlood Fillアルゴリズム。
/// </summary>
public static class FloodFillHelper
{
    public static List<(int x, int y)> Execute(TileMap tileMap, int startX, int startY)
    {
        var target = tileMap.GetTile(startX, startY);

        var result = new List<(int, int)>();
        var visited = new HashSet<(int, int)>();
        var stack = new Stack<(int x, int y)>();

        stack.Push((startX, startY));

        while (stack.Count > 0)
        {
            var (x, y) = stack.Pop();

            // 範囲チェック
            if (x < 0 || x >= tileMap.Width ||
                y < 0 || y >= tileMap.Height)
                continue;

            if (visited.Contains((x, y)))
                continue;

            visited.Add((x, y));

            if (tileMap.GetTile(x, y) != target)
                continue;

            result.Add((x, y));

            stack.Push((x + 1, y));
            stack.Push((x - 1, y));
            stack.Push((x, y + 1));
            stack.Push((x, y - 1));
        }

        return result;
    }
}
