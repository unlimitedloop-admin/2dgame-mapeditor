using StageSmith.Core.Models;

namespace StageSmith.Application.Commands;

public sealed class AddPageCommand : ICommand
{
    private readonly Stage _stage;
    private readonly string? _name;
    private readonly EditorContext _context;

    private Page? _page;
    private int _insertIndex;
    private int _selectedIndexBeforeExecute;

    public Page? AddedPage => _page;

    public AddPageCommand(Stage stage, EditorContext context, string? name = null)
    {
        _stage = stage;
        _context = context;
        _name = name;
    }

    public void Execute()
    {
        _selectedIndexBeforeExecute = _context.CurrentPageIndex;

        if (_page == null)
        {
            _page = _stage.AddPage(_name);
            _insertIndex = _stage.Pages.IndexOf(_page);
        }
        else
        {
            _stage.Pages.Insert(_insertIndex, _page);
        }

        // 追加したページへ表示を切り替える
        _context.SetPage(_insertIndex);
    }

    public void Undo()
    {
        if (_page == null) return;

        _stage.Pages.Remove(_page);

        if (_stage.Pages.Count == 0) return;

        var restoreIndex = Math.Clamp(_selectedIndexBeforeExecute, 0, _stage.Pages.Count - 1);
        _context.SetPage(restoreIndex);
    }
}
