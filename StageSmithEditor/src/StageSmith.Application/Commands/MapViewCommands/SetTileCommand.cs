using StageSmith.Core.Models;

namespace StageSmith.Application.Commands;

public class SetTileCommand : ICommand
{
    private TileMap _map;
    private int _x, _y;
    private byte _oldValue;
    private byte _newValue;

    public SetTileCommand(TileMap map, int x, int y, byte newValue)
    {
        _map = map;
        _x = x;
        _y = y;

        _oldValue = map.GetTile(x, y);
        _newValue = newValue;
    }

    public void Execute()
    {
        _map.SetTile(_x, _y, _newValue);
    }

    public void Undo()
    {
        _map.SetTile(_x, _y, _oldValue);
    }
}
