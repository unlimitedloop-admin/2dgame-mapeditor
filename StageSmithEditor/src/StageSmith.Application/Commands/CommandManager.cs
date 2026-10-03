namespace StageSmith.Application.Commands;

/// <summary>
/// コマンドの実行履歴を管理するクラス。
/// </summary>
public class CommandManager
{
    private readonly Stack<ICommand> _undoStack = new();
    private readonly Stack<ICommand> _redoStack = new();

    public event Action? HistoryChanged;

    public bool IsReadOnly { get; set; }

    /// <summary>
    /// コマンドの実行・Undo・Redo・履歴クリアの最中（HistoryChanged の通知中を含む）かどうか。
    /// この間に起きたモデルの変更は Undo 履歴で追跡されているので、
    /// 「履歴に残らない変更」と区別するために使う（MainForm の未保存判定）。
    /// </summary>
    public bool IsApplying { get; private set; }

    public void Execute(ICommand command)
    {
        if (IsReadOnly) return;

        Apply(() =>
        {
            command.Execute();
            _undoStack.Push(command);
            _redoStack.Clear();
        });
    }

    public void Undo()
    {
        if (IsReadOnly) return;
        if (_undoStack.Count == 0) return;

        Apply(() =>
        {
            var cmd = _undoStack.Pop();
            cmd.Undo();
            _redoStack.Push(cmd);
        });
    }

    public void Redo()
    {
        if (IsReadOnly) return;
        if (_redoStack.Count == 0) return;

        Apply(() =>
        {
            var cmd = _redoStack.Pop();
            cmd.Execute();
            _undoStack.Push(cmd);
        });
    }

    public void Clear()
    {
        Apply(() =>
        {
            _undoStack.Clear();
            _redoStack.Clear();
        });
    }

    public bool CanUndo => _undoStack.Count > 0;
    public bool CanRedo => _redoStack.Count > 0;

    public int UndoCount => _undoStack.Count;

    private void Apply(Action change)
    {
        var wasApplying = IsApplying;
        IsApplying = true;
        try
        {
            change();
            HistoryChanged?.Invoke();
        }
        finally
        {
            IsApplying = wasApplying;
        }
    }
}
