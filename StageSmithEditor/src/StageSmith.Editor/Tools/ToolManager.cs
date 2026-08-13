namespace StageSmith.Editor.Tools;

/// <summary>
/// ツールの管理を行うクラス。
/// IToolを継承したツールを切り替え、現在のツールを保持する。
/// </summary>
public class ToolManager
{
    public ITool? CurrentTool { get; private set; }

    /// <summary>
    /// 現在のツールが変更されたときに発生するイベント。
    /// 引数は新しいツール。
    /// </summary>
    public event Action<ITool>? ToolChanged;

    public void SetTool(ITool tool)
    {
        CurrentTool = tool;
        ToolChanged?.Invoke(tool);
    }
}
