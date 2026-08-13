using StageSmith.Core.Models;
using StageSmith.Editor.Utilities;

namespace StageSmith.Editor.Tools;

/// <summary>
/// 塗りつぶしツール。
/// </summary>
public class FillTool : ITool
{
    private readonly Func<TileMap?> _getTileMap;
    private readonly Action<List<(int x, int y)>> _applyFill;
    private readonly int _selectedTileId = -1;

    private static Cursor? _bucketCursor;

    public FillTool(Func<TileMap?> getTileMap, Action<List<(int x, int y)>> applyFill, int selectedTileId)
    {
        _getTileMap = getTileMap;
        _applyFill = applyFill;
        _selectedTileId = selectedTileId;
    }

    public void OnMouseDown(int x, int y)
    {
        var tileMap = _getTileMap();
        if (tileMap == null) return;

        var target = tileMap.GetTile(x, y);
        if (target == _selectedTileId) return;

        var fillPositions = FloodFillHelper.Execute(tileMap, x, y);

        if (fillPositions.Count > 0)
        {
            _applyFill(fillPositions);
        }
    }

    public void OnMouseMove(int x, int y) { }
    public void OnMouseUp(int x, int y) { }

    public Cursor GetCursor(int x, int y)
    {
        _bucketCursor ??= CursorFactory.FromPng(@"resource/cur/icons8-塗りつぶしの色-24.png", 21, 21);

        return _bucketCursor;
    }
}
