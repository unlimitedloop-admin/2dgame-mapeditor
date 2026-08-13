using StageSmith.Application.Commands;
using StageSmith.Core.Models;

namespace StageSmith.Editor.Tools;

/// <summary>
/// ペンツール。
/// 選択されたタイルをマップ上に描画する。
/// </summary>
public class PenTool : ITool
{
    private readonly Func<TileMap?> _getTileMap;
    private readonly Func<int> _getSelectedTileId;
    private readonly CommandManager _commandManager;
    private readonly Action _invalidate;

    private DragPaintCommand? _currentCommand;

    public PenTool(
        Func<TileMap?> getTileMap,
        Func<int> getSelectedTileId,
        CommandManager commandManager,
        Action invalidate)
    {
        _getTileMap = getTileMap;
        _getSelectedTileId = getSelectedTileId;
        _commandManager = commandManager;
        _invalidate = invalidate;
    }

    public void OnMouseDown(int x, int y)
    {
        var map = _getTileMap();
        var tileId = _getSelectedTileId();

        if (map == null || tileId < 0)
            return;

        _currentCommand = new DragPaintCommand(map);
        _currentCommand.Add(x, y, (byte)tileId);

        _invalidate();
    }

    public void OnMouseMove(int x, int y)
    {
        if (_currentCommand == null)
            return;

        var tileId = _getSelectedTileId();

        if (tileId < 0)
            return;

        _currentCommand.Add(x, y, (byte)tileId);

        _invalidate();
    }

    public void OnMouseUp(int x, int y)
    {
        if (_currentCommand != null && _currentCommand.HasChanges)
        {
            _commandManager.Execute(_currentCommand);
            _invalidate();
        }

        _currentCommand = null;
    }

    public Cursor GetCursor(int x, int y)
    {
        return Cursors.Default;
    }
}
