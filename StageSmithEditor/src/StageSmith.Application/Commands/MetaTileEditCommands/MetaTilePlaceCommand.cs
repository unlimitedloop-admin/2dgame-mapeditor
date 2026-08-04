using StageSmith.Core.Models;

namespace StageSmith.Application.Commands;

/// <summary>
/// MapView上にメタタイルを1回配置するコマンド。
/// 0xFFセルは透明扱いとして配置対象外にする。
/// </summary>
public sealed class MetaTilePlaceCommand : ICommand
{
    private readonly TileMap _tileMap;
    private readonly List<(int x, int y, byte oldValue, byte newValue)> _changes = [];

    public MetaTilePlaceCommand(TileMap tileMap, MetaTile metaTile, int originX, int originY)
    {
        _tileMap = tileMap;
        metaTile.Normalize();

        for (var y = 0; y < metaTile.Height; y++)
        {
            for (var x = 0; x < metaTile.Width; x++)
            {
                var tileId = metaTile.GetTile(x, y);

                // 0xFF は「配置しない」透明セル。
                if (tileId == MetaTile.EmptyTile)
                    continue;

                var mapX = originX + x;
                var mapY = originY + y;

                if (!IsInsideMap(mapX, mapY))
                    continue;

                var oldValue = _tileMap.GetTile(mapX, mapY);

                if (oldValue == tileId)
                    continue;

                _changes.Add((mapX, mapY, oldValue, tileId));
            }
        }
    }

    public bool HasChanges => _changes.Count > 0;

    public void Execute()
    {
        foreach (var (x, y, _, newValue) in _changes)
        {
            _tileMap.SetTile(x, y, newValue);
        }
    }

    public void Undo()
    {
        for (var i = _changes.Count - 1; i >= 0; i--)
        {
            var (x, y, oldValue, _) = _changes[i];
            _tileMap.SetTile(x, y, oldValue);
        }
    }

    private bool IsInsideMap(int x, int y)
    {
        return x >= 0
            && x < _tileMap.Width
            && y >= 0
            && y < _tileMap.Height;
    }
}
