using StageSmith.Core.Models;

namespace StageSmith.Application.Commands;

/// <summary>
/// 単一のタイルを塗り替えるコマンドです。
/// </summary>
public class SetTileCommand : ICommand
{
    private readonly TileMap _map;
    private readonly int _x, _y;
    private readonly byte _oldValue;
    private readonly byte _newValue;

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
