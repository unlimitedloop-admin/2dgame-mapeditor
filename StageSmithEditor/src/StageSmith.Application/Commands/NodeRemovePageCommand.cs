using StageSmith.Core.Models;

namespace StageSmith.Application.Commands;

/// <summary>
/// ノードエディタ上でページを削除するコマンド。
/// Undo時はページをリストの元の位置に復元する。
/// </summary>
public class NodeRemovePageCommand : ICommand
{
    private readonly Stage  _stage;
    private readonly Page   _page;
    private readonly int    _originalIndex;
    private readonly Action _onChanged;

    public NodeRemovePageCommand(Stage stage, Page page, Action onChanged)
    {
        _stage         = stage;
        _page          = page;
        _originalIndex = stage.Pages.IndexOf(page);
        _onChanged     = onChanged;
    }

    public void Execute()
    {
        _stage.Pages.Remove(_page);
        _onChanged();
    }

    public void Undo()
    {
        // 元のインデックスに挿入して順序を保つ
        var insertAt = Math.Clamp(_originalIndex, 0, _stage.Pages.Count);
        _stage.Pages.Insert(insertAt, _page);
        _onChanged();
    }
}
