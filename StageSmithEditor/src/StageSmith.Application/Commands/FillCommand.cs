using StageSmith.Core.Models;

namespace StageSmith.Application.Commands;

public class FillCommand : ICommand
{
    private readonly TileMap _map;

    private readonly List<(int x, int y, byte oldValue, byte newValue)> _changes;

    public FillCommand(TileMap map, List<(int x, int y)> positions, byte newValue)
    {
        _map = map;

        _changes = new List<(int, int, byte, byte)>(positions.Count);

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
