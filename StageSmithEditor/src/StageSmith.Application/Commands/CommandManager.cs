namespace StageSmith.Application.Commands;

public class CommandManager
{
    private Stack<ICommand> _undoStack = new();
    private Stack<ICommand> _redoStack = new();

    /// <summary>
    /// Execute・Undo・Redoが実行されたとき発火する。
    /// 購読者（MainFormなど）はこのイベントでビューを更新する。
    /// </summary>
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

    public bool CanUndo => _undoStack.Count > 0;
    public bool CanRedo => _redoStack.Count > 0;
}
