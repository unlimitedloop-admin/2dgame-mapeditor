using StageSmith.Core.Models;

namespace StageSmith.Application.Commands;

public readonly record struct MetaTileCellChange(
    int X,
    int Y,
    byte BeforeTileId,
    byte AfterTileId
);

/// <summary>
/// MetaTileの複数セルをまとめて塗り替えるコマンド。
/// </summary>
public sealed class MetaTileBatchPaintCommand : ICommand
{
    private readonly MetaTile _metaTile;
    private readonly IReadOnlyList<MetaTileCellChange> _changes;
    private readonly Action? _onChanged;

    public MetaTileBatchPaintCommand(
        MetaTile metaTile,
        IReadOnlyList<MetaTileCellChange> changes,
        Action? onChanged = null)
    {
        _metaTile = metaTile;
        _changes = changes;
        _onChanged = onChanged;
    }

    public void Execute()
    {
        foreach (var change in _changes)
        {
            _metaTile.SetTile(change.X, change.Y, change.AfterTileId);
        }

        _onChanged?.Invoke();
    }

    public void Undo()
    {
        for (var i = _changes.Count - 1; i >= 0; i--)
        {
            var change = _changes[i];
            _metaTile.SetTile(change.X, change.Y, change.BeforeTileId);
        }

        _onChanged?.Invoke();
    }
}
