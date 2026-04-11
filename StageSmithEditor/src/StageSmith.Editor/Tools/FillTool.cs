using StageSmith.Core.Models;

namespace StageSmith.Editor.Tools;

public class FillTool : ITool
{
    private readonly Func<TileMap?> _getTileMap;
    private readonly Action<List<(int x, int y)>> _applyFill;

    public FillTool(
        Func<TileMap?> getTileMap,
        Action<List<(int x, int y)>> applyFill)
    {
        _getTileMap = getTileMap;
        _applyFill = applyFill;
    }

    public void OnMouseDown(int x, int y)
    {
        var tileMap = _getTileMap();
        if (tileMap == null) return;

        var target = tileMap.GetTile(x, y);

        var fillPositions = FloodFill(x, y, target);

        if (fillPositions.Count > 0)
        {
            _applyFill(fillPositions);
        }
    }

    public void OnMouseMove(int x, int y) { }
    public void OnMouseUp(int x, int y) { }

    private List<(int x, int y)> FloodFill(int startX, int startY, int targetTile)
    {
        var result = new List<(int, int)>();
        var visited = new HashSet<(int, int)>();
        var stack = new Stack<(int x, int y)>();

        var tileMap = _getTileMap();
        if (tileMap == null) return result;

        stack.Push((startX, startY));

        while (stack.Count > 0)
        {
            var (x, y) = stack.Pop();

            // 🔥 ① 範囲チェック（最重要）
            if (x < 0 || x >= tileMap.Width ||
                y < 0 || y >= tileMap.Height)
                continue;

            // 🔥 ② 訪問済みチェック
            if (visited.Contains((x, y)))
                continue;

            visited.Add((x, y));

            // 🔥 ③ タイル一致チェック
            if (tileMap.GetTile(x, y) != targetTile)
                continue;

            result.Add((x, y));

            // 🔥 ④ 周囲追加
            stack.Push((x + 1, y));
            stack.Push((x - 1, y));
            stack.Push((x, y + 1));
            stack.Push((x, y - 1));
        }

        return result;
    }
}
