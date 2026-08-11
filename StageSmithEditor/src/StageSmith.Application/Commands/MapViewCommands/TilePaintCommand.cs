using StageSmith.Core.Models;

namespace StageSmith.Application.Commands;

/// <summary>
/// 複数のタイルを一度に塗り替えるコマンドです。
/// </summary>
public class TilePaintCommand : ICommand
{
    private readonly TileMap _map;
    private readonly List<(int x, int y, byte oldValue, byte newValue)> _changes;

    public TilePaintCommand(
        TileMap map,
        IEnumerable<(int x, int y)> positions,
        byte newValue)
    {
        _map = map;
        _changes = [];

        foreach (var (x, y) in positions)
        {
            var oldValue = map.GetTile(x, y);

            if (oldValue == newValue)
                continue;

            _changes.Add((x, y, oldValue, newValue));
        }
    }

    public void Execute()
    {
        foreach (var (x, y, _, newValue) in _changes)
        {
            _map.SetTile(x, y, newValue);
        }
    }

    public void Undo()
    {
        foreach (var (x, y, oldValue, _) in _changes)
        {
            _map.SetTile(x, y, oldValue);
        }
    }
}
