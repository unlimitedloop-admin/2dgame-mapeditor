using StageSmith.Core.Models;

namespace StageSmith.Application.Commands;

/// <summary>
/// MetaTileの全セルをクリアするコマンド。
/// </summary>
public sealed class MetaTileClearCommand : ICommand
{
    private readonly MetaTile _metaTile;
    private readonly byte[] _beforeTiles;
    private readonly Action? _onChanged;

    public MetaTileClearCommand(MetaTile metaTile, Action? onChanged = null)
    {
        _metaTile = metaTile;
        _beforeTiles = [.. metaTile.Tiles];
        _onChanged = onChanged;
    }

    public void Execute()
    {
        _metaTile.Clear();
        _onChanged?.Invoke();
    }

    public void Undo()
    {
        for (var y = 0; y < _metaTile.Height; y++)
        {
            for (var x = 0; x < _metaTile.Width; x++)
            {
                var index = y * _metaTile.Width + x;
                _metaTile.SetTile(x, y, _beforeTiles[index]);
            }
        }

        _onChanged?.Invoke();
    }
}
