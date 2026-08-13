namespace StageSmith.Application.Commands;

/// <summary>
/// 複数のコマンドをまとめて実行するためのコマンドです。
/// </summary>
public class CompositeCommand : ICommand
{
    private readonly List<ICommand> _commands;

    public CompositeCommand(List<ICommand> commands)
    {
        _commands = commands;
    }

    public void Execute()
    {
        foreach (var cmd in _commands)
        {
            cmd.Execute();
        }
    }

    public void Undo()
    {
        for (var i = _commands.Count - 1; i >= 0; i--)
        {
            _commands[i].Undo();
        }
    }
}
