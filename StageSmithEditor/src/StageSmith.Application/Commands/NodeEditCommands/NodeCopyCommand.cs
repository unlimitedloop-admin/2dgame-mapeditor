using StageSmith.Core.Models;

namespace StageSmith.Application.Commands;

/// <summary>
/// ノードエディタ上でページを複製するコマンド。
/// Ctrl+Shift+ドラッグで発生する。
/// </summary>
public class NodeCopyCommand : ICommand
{
    private readonly Stage  _stage;
    private readonly Page   _newPage;
    private readonly Action _onChanged;

    public NodeCopyCommand(Stage stage, Page newPage, Action onChanged)
    {
        _stage     = stage;
        _newPage   = newPage;
        _onChanged = onChanged;
    }

    public void Execute()
    {
        _stage.Pages.Add(_newPage);
        _onChanged();
    }

    public void Undo()
    {
        _stage.Pages.Remove(_newPage);
        _onChanged();
    }
}
