using StageSmith.Core.Models;

namespace StageSmith.Application.Commands;

/// <summary>
/// ステージのプレイヤー開始位置（.def の stage.start）を設定・解除するコマンド。
/// null を渡すと解除する。
/// </summary>
public sealed class PlayerStartSetCommand : ICommand
{
    private readonly Stage _stage;
    private readonly PlayerStart? _oldValue;
    private readonly PlayerStart? _newValue;

    public PlayerStartSetCommand(Stage stage, PlayerStart? newValue)
    {
        _stage = stage;
        _oldValue = stage.PlayerStart?.Clone();
        _newValue = newValue?.Clone();
    }

    public bool HasChanges =>
        _oldValue?.PageId != _newValue?.PageId ||
        _oldValue?.X != _newValue?.X ||
        _oldValue?.Y != _newValue?.Y;

    public void Execute() => _stage.PlayerStart = _newValue?.Clone();

    public void Undo() => _stage.PlayerStart = _oldValue?.Clone();
}
