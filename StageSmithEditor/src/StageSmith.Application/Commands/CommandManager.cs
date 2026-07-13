namespace StageSmith.Application.Commands;

public class CommandManager
{
    private readonly Stack<ICommand> _undoStack = new();
    private readonly Stack<ICommand> _redoStack = new();

    public event Action? HistoryChanged;

    public void Execute(ICommand command)
    {
        command.Execute();
        _undoStack.Push(command);
        _redoStack.Clear();
        HistoryChanged?.Invoke();
    }

    public void Undo()
    {
        if (_undoStack.Count == 0) return;

        var cmd = _undoStack.Pop();
        cmd.Undo();
        _redoStack.Push(cmd);
        HistoryChanged?.Invoke();
    }

    public void Redo()
    {
        if (_redoStack.Count == 0) return;

        var cmd = _redoStack.Pop();
        cmd.Execute();
        _undoStack.Push(cmd);
        HistoryChanged?.Invoke();
    }

    public void Clear()
    {
        _undoStack.Clear();
        _redoStack.Clear();
        HistoryChanged?.Invoke();
    }

    public bool CanUndo => _undoStack.Count > 0;
    public bool CanRedo => _redoStack.Count > 0;

    /// <summary>
    /// 現在のUndoStackの深さ。保存時点との差分比較（Dirty判定）に使用する。
    /// </summary>
    public int UndoCount => _undoStack.Count;
}
