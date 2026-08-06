using StageSmith.Core.Models;

namespace StageSmith.Application.Commands;

/// <summary>
/// ドラッグ塗りつぶし操作を表すコマンドです。
/// </summary>
public class DragPaintCommand : ICommand
{
    private readonly TileMap _tileMap;

    private readonly List<(int x, int y, byte oldValue, byte newValue)> _changes = [];

    public DragPaintCommand(TileMap tileMap)
    {
        _tileMap = tileMap;
    }

    public void Add(int x, int y, byte newValue)
    {
        var oldValue = _tileMap.GetTile(x, y);

        if (oldValue == newValue)
            return;

        // 同じ座標を重複登録しないようにする
        if (_changes.Any(c => c.x == x && c.y == y))
            return;

        _changes.Add((x, y, oldValue, newValue));
    }

    public void Execute()
    {
        foreach (var (x, y, _, newValue) in _changes)
        {
            _tileMap.SetTile(x, y, newValue);
        }
    }

    public void Undo()
    {
        foreach (var (x, y, oldValue, _) in _changes)
        {
            _tileMap.SetTile(x, y, oldValue);
        }
    }

    public bool HasChanges => _changes.Count > 0;
}
