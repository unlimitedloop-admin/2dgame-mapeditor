using StageSmith.Core.Models;

namespace StageSmith.Application.Commands;

/// <summary>
/// ブックマークを削除するコマンドです。
/// </summary>
public sealed class RemoveBookmarkCommand : ICommand
{
    private readonly EditorProject _project;
    private readonly Bookmark _bookmark;

    public RemoveBookmarkCommand(EditorProject project, Bookmark bookmark)
    {
        _project = project;
        _bookmark = bookmark;
    }

    public void Execute() => _project.Bookmarks.Remove(_bookmark);
    public void Undo() => _project.Bookmarks.Add(_bookmark);
}
