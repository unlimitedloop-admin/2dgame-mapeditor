using StageSmith.Application.Commands;
using StageSmith.Core.Models;
using StageSmith.Editor.Controls;

namespace StageSmith.Editor;

public partial class MainForm
{
    private void InitializeBookmarkEvents()
    {
        _stageExplorer.BookmarkToggleRequested += ToggleBookmark;
        _mapView.ContextMenuRequested += OnMapViewContextMenuRequested;

        _bookmarkList.PageJumpRequested += (stage, page) =>
        {
            if (_context.Project == null) return;
            _context.SetStage(_context.Project.Stages.IndexOf(stage));
            _context.SetPage(stage.Pages.IndexOf(page));
        };

        _bookmarkList.BookmarkDeleteRequested += bookmark =>
        {
            if (_context.Project == null) return;
            _commandManager.Execute(new RemoveBookmarkCommand(_context.Project, bookmark));
        };

        _bookmarkList.BookmarkEditRequested += (bookmark, label, description) =>
        {
            _commandManager.Execute(new EditBookmarkCommand(bookmark, label, description));
        };
    }

    private void BindBookmarkList()
    {
        _bookmarkList.Bind(_context.Project);
    }

    private void OnMapViewContextMenuRequested(object? sender, TileContextMenuEventArgs e)
    {
        var stage = _context.CurrentStage;
        var page = _context.CurrentPage;
        if (stage == null || page == null) return;

        var isBookmarked = _context.Project?.Bookmarks
            .Any(b => b.StageId == stage.Id && b.PageId == page.Id) ?? false;

        // NOTE: 拡大率・ページ指定などは今回のスコープ外。
        // それらを足す際は毎回生成ではなく、StageExplorerと同様に
        // 永続メニュー + Opening イベントでの書き換え方式へリファクタ予定。
        var menu = new ContextMenuStrip();
        menu.Items.Add(
            isBookmarked ? "ブックマークを解除" : "現在のページをブックマークに追加",
            null,
            (_, _) => ToggleBookmark(stage, page));

        menu.Show(e.ScreenLocation);
    }

    private void ToggleBookmark(Stage stage, Page page)
    {
        if (_context.Project == null) return;

        var existing = _context.Project.Bookmarks
            .FirstOrDefault(b => b.StageId == stage.Id && b.PageId == page.Id);

        if (existing != null)
        {
            _commandManager.Execute(new RemoveBookmarkCommand(_context.Project, existing));
            return;
        }

        var pageIndex = stage.Pages.IndexOf(page);
        var defaultLabel = string.IsNullOrWhiteSpace(page.Name)
            ? $"Page {pageIndex:D2}"
            : page.Name;

        var bookmark = new Bookmark
        {
            StageId = stage.Id,
            PageId = page.Id,
            Label = defaultLabel
        };

        _commandManager.Execute(new AddBookmarkCommand(_context.Project, bookmark));
    }

    private void ToggleBookmarkForCurrentPage()
    {
        if (_context.CurrentStage is { } stage && _context.CurrentPage is { } page)
            ToggleBookmark(stage, page);
    }

    private void ClearAllBookmarks()
    {
        var project = _context.Project;
        if (project == null || project.Bookmarks.Count == 0) return;

        var confirm = MessageBox.Show(
            this,
            $"全てのブックマーク（{project.Bookmarks.Count}件）を削除します。よろしいですか？",
            "Clear All Bookmarks",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning);

        if (confirm != DialogResult.Yes) return;

        var commands = project.Bookmarks
            .Select(b => (ICommand)new RemoveBookmarkCommand(project, b))
            .ToList();

        _commandManager.Execute(new CompositeCommand(commands));
    }
}
