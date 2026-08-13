namespace StageSmith.Editor.Utilities;

/// <summary>
/// MenuStrip の実際の定義からショートカット一覧を収集する。
/// メニュー定義とショートカット一覧を二重管理しないための仕組み。
/// </summary>
internal static class MenuShortcutCollector
{
    public readonly record struct ShortcutEntry(string MenuName, string CommandName, string Shortcut);

    private static readonly KeysConverter _keysConverter = new();

    public static IReadOnlyList<ShortcutEntry> Collect(MenuStrip menuStrip)
    {
        var result = new List<ShortcutEntry>();

        foreach (ToolStripItem topItem in menuStrip.Items)
        {
            if (topItem is not ToolStripMenuItem topMenu) continue;
            CollectFromMenu(topMenu.Text ?? string.Empty, topMenu, result);
        }

        return result;
    }

    private static void CollectFromMenu(string menuName, ToolStripMenuItem parent, List<ShortcutEntry> result)
    {
        foreach (ToolStripItem item in parent.DropDownItems)
        {
            if (item is not ToolStripMenuItem menuItem) continue; // セパレータを除外

            var shortcut = GetShortcutText(menuItem);
            if (!string.IsNullOrEmpty(shortcut))
                result.Add(new ShortcutEntry(menuName, menuItem.Text ?? string.Empty, shortcut));
        }
    }

    private static string GetShortcutText(ToolStripMenuItem item)
    {
        if (item.ShortcutKeys != Keys.None)
            return _keysConverter.ConvertToString(item.ShortcutKeys) ?? string.Empty;

        return item.ShortcutKeyDisplayString ?? string.Empty;
    }
}
