using StageSmith.Core.Models;

namespace StageSmith.Application.Commands;

/// <summary>
/// 指定されたページを削除するコマンドです。
/// </summary>
public sealed class RemovePageCommand : ICommand
{
    private readonly Stage _stage;
    private readonly Page _page;
    private readonly EditorContext _context;

    private int _removedIndex;
    private bool _affectsCurrentSelection;
    private int _selectedIndexBeforeExecute;

    public RemovePageCommand(Stage stage, Page page, EditorContext context)
    {
        _stage = stage;
        _page = page;
        _context = context;
    }

    public void Execute()
    {
        _removedIndex = _stage.Pages.IndexOf(_page);

        // 削除対象が「現在表示中のページ」だった場合のみ、選択の移動もこの操作に含める
        _affectsCurrentSelection = ReferenceEquals(_context.CurrentPage, _page);
        _selectedIndexBeforeExecute = _context.CurrentPageIndex;

        _stage.Pages.Remove(_page);

        if (!_affectsCurrentSelection) return;
        if (_stage.Pages.Count == 0) return;

        var nextIndex = Math.Clamp(_removedIndex - 1, 0, _stage.Pages.Count - 1);
        _context.SetPage(nextIndex);
    }

    public void Undo()
    {
        var insertAt = Math.Min(_removedIndex, _stage.Pages.Count);
        _stage.Pages.Insert(insertAt, _page);

        if (_affectsCurrentSelection)
        {
            _context.SetPage(_selectedIndexBeforeExecute);
        }
    }
}
