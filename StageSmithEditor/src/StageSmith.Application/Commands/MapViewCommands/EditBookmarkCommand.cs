using StageSmith.Core.Models;

namespace StageSmith.Application.Commands;

/// <summary>
/// ブックマークのラベルと説明を編集するコマンドです。
/// </summary>
public sealed class EditBookmarkCommand : ICommand
{
    private readonly Bookmark _bookmark;
    private readonly string _newLabel;
    private readonly string _newDescription;

    private string _oldLabel = "";
    private string _oldDescription = "";

    public EditBookmarkCommand(Bookmark bookmark, string newLabel, string newDescription)
    {
        _bookmark = bookmark;
        _newLabel = newLabel;
        _newDescription = newDescription;
    }

    public void Execute()
    {
        _oldLabel = _bookmark.Label;
        _oldDescription = _bookmark.Description;

        _bookmark.Label = _newLabel;
        _bookmark.Description = _newDescription;
    }

    public void Undo()
    {
        _bookmark.Label = _oldLabel;
        _bookmark.Description = _oldDescription;
    }
}
