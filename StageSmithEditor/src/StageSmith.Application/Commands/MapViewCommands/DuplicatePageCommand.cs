using StageSmith.Core.Models;

namespace StageSmith.Application.Commands;

public sealed class DuplicatePageCommand : ICommand
{
    private readonly Stage _stage;
    private readonly Page _sourcePage;
    private readonly EditorContext _context;

    private Page? _clone;
    private int _insertIndex;
    private int _selectedIndexBeforeExecute;

    public Page? DuplicatedPage => _clone;

    public DuplicatePageCommand(Stage stage, Page sourcePage, EditorContext context)
    {
        _stage = stage;
        _sourcePage = sourcePage;
        _context = context;
    }

    public void Execute()
    {
        _selectedIndexBeforeExecute = _context.CurrentPageIndex;

        if (_clone == null)
        {
            _clone = _sourcePage.Clone();
            _insertIndex = _stage.Pages.IndexOf(_sourcePage) + 1;
        }

        _stage.Pages.Insert(_insertIndex, _clone);
        _stage.MarkDirty();

        // 複製したページへ表示を切り替える
        _context.SetPage(_insertIndex);
    }

    public void Undo()
    {
        if (_clone == null) return;

        _stage.Pages.Remove(_clone);

        if (_stage.Pages.Count == 0) return;

        var restoreIndex = Math.Clamp(_selectedIndexBeforeExecute, 0, _stage.Pages.Count - 1);
        _context.SetPage(restoreIndex);
    }
}
