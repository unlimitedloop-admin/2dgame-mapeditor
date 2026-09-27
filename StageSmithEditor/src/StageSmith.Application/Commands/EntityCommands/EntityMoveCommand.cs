using StageSmith.Core.Models;

namespace StageSmith.Application.Commands;

/// <summary>
/// エンティティの位置（足元中心の部屋内ピクセル）を変更するコマンド。
/// ドラッグ中は呼び出し側で位置を即時反映しておき、確定時にこのコマンドを積む
/// （Execute は新しい位置を代入するだけなので、反映済みでも二重に動かない）。
/// </summary>
public sealed class EntityMoveCommand : ICommand
{
    private readonly EntityPlacement _entity;
    private readonly (int X, int Y) _from;
    private readonly (int X, int Y) _to;

    public EntityMoveCommand(EntityPlacement entity, (int X, int Y) from, (int X, int Y) to)
    {
        _entity = entity;
        _from = from;
        _to = to;
    }

    public bool HasChanges => _from != _to;

    public void Execute()
    {
        _entity.X = _to.X;
        _entity.Y = _to.Y;
    }

    public void Undo()
    {
        _entity.X = _from.X;
        _entity.Y = _from.Y;
    }
}
