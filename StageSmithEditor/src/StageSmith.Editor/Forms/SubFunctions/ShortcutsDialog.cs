using StageSmith.Editor.Utilities;

namespace StageSmith.Editor;

public class ShortcutsDialog : Form
{
    public ShortcutsDialog(MenuStrip menuStrip)
    {
        Text = "Shortcuts";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MinimizeBox = false;
        MaximizeBox = false;
        ClientSize = new Size(420, 520);

        var listView = new ListView
        {
            Location = new Point(12, 12),
            Size = new Size(396, 456),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom,
            View = View.Details,
            FullRowSelect = true,
            GridLines = true,
        };
        listView.Columns.Add("Command", 240);
        listView.Columns.Add("Shortcut", 130);

        foreach (var group in MenuShortcutCollector.Collect(menuStrip).GroupBy(e => e.MenuName))
        {
            var lvGroup = new ListViewGroup(group.Key);
            listView.Groups.Add(lvGroup);

            foreach (var entry in group)
            {
                var lvItem = new ListViewItem(entry.CommandName) { Group = lvGroup };
                lvItem.SubItems.Add(entry.Shortcut);
                listView.Items.Add(lvItem);
            }
        }

        var closeButton = new Button
        {
            Text = "Close",
            DialogResult = DialogResult.Cancel,
            Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
            Location = new Point(333, 480),
        };

        Controls.Add(listView);
        Controls.Add(closeButton);

        CancelButton = closeButton;
    }
}
