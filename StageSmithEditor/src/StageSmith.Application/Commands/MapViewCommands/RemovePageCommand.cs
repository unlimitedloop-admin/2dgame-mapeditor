using StageSmith.Core.Models;

namespace StageSmith.Application.Commands;

/// <summary>
/// ステージからのページ削除をUndo/Redo対応にするコマンド。
/// </summary>
public sealed class RemovePageCommand : ICommand
{
    private readonly Stage _stage;
    private readonly Page _page;
    private int _index;

    public RemovePageCommand(Stage stage, Page page)
    {
        _stage = stage;
        _page = page;
    }

    public void Execute()
    {
        _index = _stage.Pages.IndexOf(_page);
        _stage.Pages.Remove(_page);
    }

    public void Undo()
    {
        var insertAt = Math.Min(_index, _stage.Pages.Count);
        _stage.Pages.Insert(insertAt, _page);
    }
}
