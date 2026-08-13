using StageSmith.Core.Models;

namespace StageSmith.Application.Commands;

/// <summary>
/// MetaTileの1セルを塗り替えるコマンド。
/// </summary>
public sealed class MetaTilePaintCommand : ICommand
{
    private readonly MetaTile _metaTile;
    private readonly int _x;
    private readonly int _y;
    private readonly byte _beforeTileId;
    private readonly byte _afterTileId;
    private readonly Action? _onChanged;

    public MetaTilePaintCommand(
        MetaTile metaTile,
        int x,
        int y,
        byte beforeTileId,
        byte afterTileId,
        Action? onChanged = null)
    {
        _metaTile = metaTile;
        _x = x;
        _y = y;
        _beforeTileId = beforeTileId;
        _afterTileId = afterTileId;
        _onChanged = onChanged;
    }

    public void Execute()
    {
        _metaTile.SetTile(_x, _y, _afterTileId);
        _onChanged?.Invoke();
    }

    public void Undo()
    {
        _metaTile.SetTile(_x, _y, _beforeTileId);
        _onChanged?.Invoke();
    }
}
