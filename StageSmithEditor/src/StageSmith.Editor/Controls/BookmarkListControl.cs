using StageSmith.Core.Models;

namespace StageSmith.Editor.Controls;

/// <summary>
/// ブックマークの一覧を表示するコントロールです。
/// </summary>
public class BookmarkListControl : UserControl
{
    public event Action<Stage, Page>? PageJumpRequested;
    public event Action<Bookmark>? BookmarkDeleteRequested;
    public event Action<Bookmark, string, string>? BookmarkEditRequested;

    private EditorProject? _project;

    private readonly ListView _listView;
    private readonly ContextMenuStrip _itemMenu;
    private int _sortColumn = 1; // デフォルトは Page 列でソート
    private bool _sortAscending = true;

    public BookmarkListControl()
    {
        Dock = DockStyle.Fill;

        _listView = CreateListView();
        _itemMenu = CreateItemContextMenu();

        Controls.Add(_listView);
    }

    public void Bind(EditorProject? project)
    {
        _project = project;
        RefreshList();
    }

    public void RefreshList()
    {
        _listView.BeginUpdate();
        _listView.Items.Clear();

        if (_project != null)
        {
            foreach (var bookmark in _project.Bookmarks)
            {
                var stage = _project.FindStage(bookmark.StageId);
                var page = stage?.Pages.FirstOrDefault(p => p.Id == bookmark.PageId);

                // 参照先が消失している孤立ブックマークはリストに出さない
                // （ページ削除時にブックマークも一緒に消す設計にすればここは通らないはずだが念のため）
                if (stage == null || page == null) continue;

                var pageIndex = stage.Pages.IndexOf(page);

                var item = new ListViewItem(stage.Name) { Tag = bookmark };
                item.SubItems.Add($"Page {pageIndex:D2}");
                item.SubItems.Add(bookmark.Label);

                _listView.Items.Add(item);
            }
        }

        _listView.EndUpdate();
    }

    private ListView CreateListView()
    {
        var lv = new ListView
        {
            Dock = DockStyle.Fill,
            View = View.Details,
            FullRowSelect = true,
            HideSelection = false,
            MultiSelect = false,
            GridLines = true
        };

        lv.Columns.Add("Stage", 100);
        lv.Columns.Add("Page", 70);
        lv.Columns.Add("Label", 140);

        lv.ColumnClick += OnColumnClick;
        lv.DoubleClick += OnItemDoubleClick;
        lv.MouseUp += OnMouseUp;

        return lv;
    }

    private void OnColumnClick(object? sender, ColumnClickEventArgs e)
    {
        if (_sortColumn == e.Column)
        {
            _sortAscending = !_sortAscending;
        }
        else
        {
            _sortColumn = e.Column;
            _sortAscending = true;
        }

        _listView.ListViewItemSorter = new ListViewItemComparer(_sortColumn, _sortAscending);
        _listView.Sort();
    }

    private void OnItemDoubleClick(object? sender, EventArgs e)
    {
        if (_listView.SelectedItems.Count == 0) return;
        if (_listView.SelectedItems[0].Tag is not Bookmark bookmark) return;
        if (_project == null) return;

        var stage = _project.FindStage(bookmark.StageId);
        var page = stage?.Pages.FirstOrDefault(p => p.Id == bookmark.PageId);
        if (stage == null || page == null) return;

        PageJumpRequested?.Invoke(stage, page);
    }

    private void OnMouseUp(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Right) return;

        var item = _listView.GetItemAt(e.X, e.Y);
        if (item == null) return;

        item.Selected = true;
        _itemMenu.Show(_listView, e.Location);
    }

    private ContextMenuStrip CreateItemContextMenu()
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add("Edit", null, OnEditClicked);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Delete", null, OnDeleteClicked);
        return menu;
    }

    private void OnEditClicked(object? sender, EventArgs e)
    {
        if (_listView.SelectedItems.Count == 0) return;
        if (_listView.SelectedItems[0].Tag is not Bookmark bookmark) return;

        using var dialog = new BookmarkEditDialog(bookmark.Label, bookmark.Description);
        if (dialog.ShowDialog(FindForm()) != DialogResult.OK) return;

        BookmarkEditRequested?.Invoke(bookmark, dialog.Label, dialog.Description);
    }

    private void OnDeleteClicked(object? sender, EventArgs e)
    {
        if (_listView.SelectedItems.Count == 0) return;
        if (_listView.SelectedItems[0].Tag is not Bookmark bookmark) return;

        BookmarkDeleteRequested?.Invoke(bookmark);
    }

    private sealed class ListViewItemComparer(int column, bool ascending) : System.Collections.IComparer
    {
        public int Compare(object? x, object? y)
        {
            var itemX = (ListViewItem)x!;
            var itemY = (ListViewItem)y!;

            var result = string.Compare(
                itemX.SubItems[column].Text,
                itemY.SubItems[column].Text,
                StringComparison.OrdinalIgnoreCase);

            return ascending ? result : -result;
        }
    }
}
