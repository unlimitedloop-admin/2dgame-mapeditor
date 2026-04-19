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

        var fillPositions = FloodFillHelper.Execute(tileMap, x, y);

        if (fillPositions.Count > 0)
        {
            _applyFill(fillPositions);
        }
    }

    public void OnMouseMove(int x, int y) { }
    public void OnMouseUp(int x, int y) { }
}
