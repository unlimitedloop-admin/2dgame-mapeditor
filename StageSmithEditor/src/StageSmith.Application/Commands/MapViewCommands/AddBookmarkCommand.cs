using StageSmith.Core.Models;

namespace StageSmith.Application.Commands;

/// <summary>
/// ブックマークを追加するコマンドです。
/// </summary>
public sealed class AddBookmarkCommand : ICommand
{
    private readonly EditorProject _project;
    private readonly Bookmark _bookmark;

    public AddBookmarkCommand(EditorProject project, Bookmark bookmark)
    {
        _project = project;
        _bookmark = bookmark;
    }

    public void Execute() => _project.Bookmarks.Add(_bookmark);
    public void Undo() => _project.Bookmarks.Remove(_bookmark);
}
