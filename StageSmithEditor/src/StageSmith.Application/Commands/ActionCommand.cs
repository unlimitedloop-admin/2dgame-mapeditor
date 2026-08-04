namespace StageSmith.Application.Commands;

/// <summary>
/// Execute/Undoの処理をデリゲートとして受け取る汎用コマンド。
/// フィールド単位の値変更など、専用コマンドクラスを都度作るまでもない変更に使う。
/// </summary>
public sealed class ActionCommand : ICommand
{
    private readonly Action _execute;
    private readonly Action _undo;

    public ActionCommand(Action execute, Action undo)
    {
        _execute = execute;
        _undo = undo;
    }

    public void Execute() => _execute();
    public void Undo() => _undo();
}
