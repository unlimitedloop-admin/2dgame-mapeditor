using StageSmith.Editor.Tools;

namespace StageSmith.Application.Commands;

public class ChangeToolCommand : ICommand
{
    private readonly ToolManager _toolManager;
    private readonly ITool _newTool;
    private ITool? _oldTool;

    public ChangeToolCommand(ToolManager toolManager, ITool newTool)
    {
        _toolManager = toolManager;
        _newTool = newTool;
    }

    public void Execute()
    {
        _oldTool = _toolManager.CurrentTool;
        _toolManager.SetTool(_newTool);
    }

    public void Undo()
    {
        if (_oldTool != null)
            _toolManager.SetTool(_oldTool);
    }
}
