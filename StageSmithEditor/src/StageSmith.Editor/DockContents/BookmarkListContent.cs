using WeifenLuo.WinFormsUI.Docking;
using StageSmith.Editor.Controls;

namespace StageSmith.Editor.DockContents;

public class BookmarkListContent : DockContent
{
    public BookmarkListControl BookmarkList { get; }

    public BookmarkListContent()
    {
        Text = "Bookmark List";
        HideOnClose = true;
        CloseButtonVisible = true;
        DockAreas = DockAreas.DockLeft | DockAreas.DockRight | DockAreas.Float;

        BookmarkList = new BookmarkListControl { Dock = DockStyle.Fill };
        Controls.Add(BookmarkList);
    }
}
