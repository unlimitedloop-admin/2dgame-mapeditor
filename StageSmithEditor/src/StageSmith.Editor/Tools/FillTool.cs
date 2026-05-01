using StageSmith.Core.Models;

namespace StageSmith.Editor.Tools;

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
        if (_bucketCursor == null)
        {
            _bucketCursor = new Cursor(new MemoryStream(StageSmithEditor.Properties.Resources.icons8_塗りつぶしの色_24));
        }
        return _bucketCursor;
    }
}
