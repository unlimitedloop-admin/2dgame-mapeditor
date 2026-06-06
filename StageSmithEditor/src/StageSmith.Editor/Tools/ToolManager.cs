namespace StageSmith.Editor.Tools;

public class ToolManager
{
    public ITool? CurrentTool { get; private set; }

    public event Action<ITool>? ToolChanged;

    public void SetTool(ITool tool)
    {
        CurrentTool = tool;
        ToolChanged?.Invoke(tool);
    }
}
