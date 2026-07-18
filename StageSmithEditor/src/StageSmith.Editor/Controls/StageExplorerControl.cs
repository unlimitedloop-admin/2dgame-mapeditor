using StageSmith.Core.Models;

namespace StageSmith.Editor.Controls;

/// <summary>
/// プロジェクト内のステージ・ページ構造をツリー表示するエクスプローラーパネル。
/// </summary>
public class StageExplorerControl : UserControl
{
    //========================
    // イベント
    //========================

    /// <summary>
    /// ページが選択されたとき発火する。
    /// </summary>
    public event Action<Stage, Page>? PageSelected;

    /// <summary>
    /// ステージが追加・削除・複製されたとき発火する。
    /// </summary>
    public event Action? StageListChanged;

    /// <summary>
    /// ページが追加・削除・複製されたとき発火する。
    /// </summary>
    public event Action<Stage>? PageListChanged;

    /// <summary>
    /// ページの削除がユーザーによって確認されたとき発火する。
    /// 実際のモデル操作（Undo対応含む）は呼び出し側が行う。
    /// </summary>
    public event Action<Stage, Page>? PageDeleteRequested;

    /// <summary>
    /// ブックマークの切り替えがユーザーによって要求されたとき発火する。
    /// 実際のモデル操作（Undo対応含む）は呼び出し側が行う。
    /// </summary>
    public event Action<Stage, Page>? BookmarkToggleRequested;

    //========================
    // 内部状態
    //========================
    private EditorProject? _project;
    private Stage? _currentStage;
    private Page? _currentPage;

    //========================
    // Controls
    //========================
    private readonly TreeView _treeView;
    private readonly ContextMenuStrip _stageMenu;
    private readonly ContextMenuStrip _pageMenu;
    private readonly ToolStripMenuItem _pageBookmarkMenuItem = new("Add Bookmark");
    private readonly Font _currentPageFont;
    private bool _suppressSelectEvent;

    //========================
    // アイコンインデックス（ImageList）
    //========================
    private const int IconStage = 0;
    private const int IconPage = 1;

    //========================
    // 初期化
    //========================
    public StageExplorerControl()
    {
        InitializeLayout();
        _treeView = CreateTreeView();
        _stageMenu = CreateStageContextMenu();
        _pageMenu = CreatePageContextMenu();
        _currentPageFont = new Font(_treeView.Font, FontStyle.Bold);

        _pageBookmarkMenuItem.Click += OnPageBookmarkToggle;

        Controls.Add(_treeView);
    }

    //========================
    // 公開メソッド
    //========================

    /// <summary>
    /// プロジェクトをバインドしてツリーを再構築する。
    /// </summary>
    public void Bind(EditorProject? project)
    {
        _project = project;
        RebuildTree();
    }

    /// <summary>
    /// 現在選択中のページをハイライト更新する。
    /// </summary>
    public void SetCurrentPage(Stage stage, Page page)
    {
        _currentStage = stage;
        _currentPage = page;

        var node = FindPageNode(stage, page);
        if (node != null && !ReferenceEquals(_treeView.SelectedNode, node))
        {
            _suppressSelectEvent = true;
            _treeView.SelectedNode = node;
            _suppressSelectEvent = false;
        }

        RefreshHighlight();
    }

    private TreeNode? FindPageNode(Stage stage, Page page)
    {
        foreach (TreeNode stageNode in _treeView.Nodes)
        {
            foreach (TreeNode pageNode in stageNode.Nodes)
            {
                if (pageNode.Tag is NodeTag tag
                    && ReferenceEquals(tag.Stage, stage)
                    && ReferenceEquals(tag.Page, page))
                {
                    return pageNode;
                }
            }
        }
        return null;
    }

    /// <summary>
    /// ツリーを再構築する（外部から呼べる）。
    /// </summary>
    public void RebuildTree()
    {
        _treeView.BeginUpdate();
        _treeView.Nodes.Clear();

        if (_project == null)
        {
            _treeView.EndUpdate();
            return;
        }

        foreach (var stage in _project.Stages)
        {
            var stageNode = CreateStageNode(stage);
            _treeView.Nodes.Add(stageNode);
        }

        _treeView.ExpandAll();
        _treeView.EndUpdate();

        AdjustTreeViewWidth(_treeView);

        // ノード再生成でネイティブ選択状態が失われるため、現在ページに合わせて再同期する
        if (_currentStage != null && _currentPage != null)
        {
            var node = FindPageNode(_currentStage, _currentPage);
            if (node != null)
            {
                _suppressSelectEvent = true;
                _treeView.SelectedNode = node;
                _suppressSelectEvent = false;
            }
        }

        RefreshHighlight();
    }

    //========================
    // ツリー構築
    //========================
    private static TreeNode CreateStageNode(Stage stage)
    {
        var node = new TreeNode(stage.Name)
        {
            ImageIndex = IconStage,
            SelectedImageIndex = IconStage,
            Tag = new NodeTag(NodeKind.Stage, stage, null)
        };

        for (var i = 0; i < stage.Pages.Count; i++)
        {
            var page = stage.Pages[i];
            var pageNode = CreatePageNode(stage, page, i);
            node.Nodes.Add(pageNode);
        }

        return node;
    }

    private static TreeNode CreatePageNode(Stage stage, Page page, int index)
    {
        var label = string.IsNullOrWhiteSpace(page.Name)
            ? $"Page {index:D2}"
            : $"Page {index:D2}  {page.Name}";

        return new TreeNode(label)
        {
            ImageIndex = IconPage,
            SelectedImageIndex = IconPage,
            Tag = new NodeTag(NodeKind.Page, stage, page)
        };
    }

    //========================
    // ハイライト
    //========================
    private void RefreshHighlight()
    {
        _treeView.BeginUpdate();

        foreach (TreeNode stageNode in _treeView.Nodes)
        {
            foreach (TreeNode pageNode in stageNode.Nodes)
            {
                if (pageNode.Tag is not NodeTag tag || tag.Page == null)
                    continue;

                var isCurrent = ReferenceEquals(tag.Stage, _currentStage)
                             && ReferenceEquals(tag.Page, _currentPage);

                pageNode.NodeFont = isCurrent ? _currentPageFont : null;

                pageNode.ForeColor = isCurrent
                    ? SystemColors.Highlight
                    : _treeView.ForeColor;

                // WinForms の TreeView は太字変更後にテキスト幅を再計算しないため
                // 末尾スペースを付与して右端の欠けを防ぐ
                var baseText = pageNode.Text.TrimEnd();
                pageNode.Text = isCurrent ? baseText + "  " : baseText;
            }
        }

        _treeView.EndUpdate();
        _treeView.Invalidate();
    }

    //========================
    // TreeView イベント
    //========================
    private void OnNodeMouseClick(object? sender, TreeNodeMouseClickEventArgs e)
    {
        if (e.Button == MouseButtons.Right)
        {
            if (e.Node != null)
            {
                // 右クリックでのノード選択はページ切り替えを伴わせたくないので抑制
                _suppressSelectEvent = true;
                _treeView.SelectedNode = e.Node;
                _suppressSelectEvent = false;

                ShowContextMenu(e.Node, e.Location);
            }
            return;
        }

        // 左クリックの選択自体はTreeViewが自動でSelectedNodeを更新するため
        // ページ切り替えロジックはOnAfterSelectに一本化する
    }

    private void OnBeforeLabelEdit(object? sender, NodeLabelEditEventArgs e)
    {
        // ラベル編集は右クリックメニューの Rename からのみ許可
        // 直接ダブルクリック編集はキャンセル
        if (e.Node?.Tag is NodeTag tag && tag.Kind == NodeKind.Stage)
        {
            // Stage のみ編集許可（Rename メニュー経由でも同じ処理）
            return;
        }
        e.CancelEdit = true;
    }

    private void OnAfterSelect(object? sender, TreeViewEventArgs e)
    {
        if (_suppressSelectEvent) return;
        if (e.Node?.Tag is not NodeTag tag) return;
        if (tag.Kind != NodeKind.Page || tag.Page == null) return;

        _currentStage = tag.Stage;
        _currentPage = tag.Page;
        RefreshHighlight();
        PageSelected?.Invoke(tag.Stage, tag.Page);
    }

    private void OnAfterLabelEdit(object? sender, NodeLabelEditEventArgs e)
    {
        if (e.Label == null || e.Node?.Tag is not NodeTag tag)
        {
            e.CancelEdit = true;
            return;
        }

        var newName = e.Label.Trim();
        if (string.IsNullOrEmpty(newName))
        {
            e.CancelEdit = true;
            return;
        }

        if (tag.Kind == NodeKind.Stage)
        {
            tag.Stage.Name = newName;
            StageListChanged?.Invoke();
        }
        else if (tag.Kind == NodeKind.Page && tag.Page != null)
        {
            tag.Page.Name = newName;
            PageListChanged?.Invoke(tag.Stage);
            // ページノードのテキストをラベル形式に合わせて更新
            e.CancelEdit = true;
            RebuildTree();
        }
    }

    //========================
    // コンテキストメニュー表示
    //========================
    private void ShowContextMenu(TreeNode node, Point location)
    {
        if (node.Tag is not NodeTag tag) return;

        if (tag.Kind == NodeKind.Stage)
        {
            _stageMenu.Tag = tag;
            _stageMenu.Show(_treeView, location);
        }
        else if (tag.Kind == NodeKind.Page && tag.Page != null)
        {
            _pageMenu.Tag = tag;
            UpdateBookmarkMenuItemText(tag.Stage, tag.Page);   // ← 追加
            _pageMenu.Show(_treeView, location);
        }
    }

    private void UpdateBookmarkMenuItemText(Stage stage, Page page)
    {
        var isBookmarked = _project?.Bookmarks
            .Any(b => b.StageId == stage.Id && b.PageId == page.Id) ?? false;

        _pageBookmarkMenuItem.Text = isBookmarked ? "Remove Bookmark" : "Add Bookmark";
    }

    //========================
    // Stage コンテキストメニュー操作
    //========================
    private void OnStageRename(object? sender, EventArgs e)
    {
        if (_treeView.SelectedNode == null) return;
        _treeView.LabelEdit = true;
        _treeView.SelectedNode.BeginEdit();
    }

    private void OnStageDelete(object? sender, EventArgs e)
    {
        if (_stageMenu.Tag is not NodeTag tag) return;
        if (_project == null) return;

        var result = MessageBox.Show(
            $"ステージ「{tag.Stage.Name}」を削除しますか？\nこの操作は元に戻せません。",
            "ステージの削除",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning);

        if (result != DialogResult.Yes) return;

        _project.Stages.Remove(tag.Stage);

        // 削除したステージが選択中だった場合はリセット
        if (ReferenceEquals(_currentStage, tag.Stage))
        {
            _currentStage = null;
            _currentPage = null;
        }

        RebuildTree();
        StageListChanged?.Invoke();
    }

    private void OnStageClone(object? sender, EventArgs e)
    {
        if (_stageMenu.Tag is not NodeTag tag) return;
        if (_project == null) return;

        var clone = tag.Stage.Clone();
        clone.Name = $"{tag.Stage.Name}_Copy";

        var insertIndex = _project.Stages.IndexOf(tag.Stage) + 1;
        _project.Stages.Insert(insertIndex, clone);

        RebuildTree();
        StageListChanged?.Invoke();
    }

    //========================
    // Page コンテキストメニュー操作
    //========================
    private void OnPageRename(object? sender, EventArgs e)
    {
        if (_treeView.SelectedNode == null) return;
        _treeView.LabelEdit = true;
        _treeView.SelectedNode.BeginEdit();
    }

    private void OnPageDelete(object? sender, EventArgs e)
    {
        if (_pageMenu.Tag is not NodeTag tag || tag.Page == null) return;

        var pageIndex = tag.Stage.Pages.IndexOf(tag.Page);
        var label = string.IsNullOrWhiteSpace(tag.Page.Name)
            ? $"Page {pageIndex:D2}"
            : tag.Page.Name;

        var result = MessageBox.Show(
            $"ページ「{label}」を削除しますか？",
            "ページの削除",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning);

        if (result != DialogResult.Yes) return;

        // 実際の削除・Undo登録は呼び出し側（MainForm）に委譲する
        PageDeleteRequested?.Invoke(tag.Stage, tag.Page);
    }

    private void OnPageDuplicate(object? sender, EventArgs e)
    {
        if (_pageMenu.Tag is not NodeTag tag || tag.Page == null) return;

        var clone = tag.Page.Clone();

        var insertIndex = tag.Stage.Pages.IndexOf(tag.Page) + 1;
        tag.Stage.Pages.Insert(insertIndex, clone);

        RebuildTree();
        PageListChanged?.Invoke(tag.Stage);
    }

    //========================
    // UI構築ヘルパー
    //========================
    private void InitializeLayout()
    {
        Dock = DockStyle.Fill;
        Padding = new Padding(0);
    }

    private TreeView CreateTreeView()
    {
        var tv = new TreeView
        {
            Dock = DockStyle.Fill,
            LabelEdit = false,
            HideSelection = false,
            ShowLines = true,
            ShowPlusMinus = true,
            ShowRootLines = true,
            FullRowSelect = true,
            Scrollable = true,
            ShowNodeToolTips = true,   // 途切れた文字をツールチップで表示
            ImageList = CreateImageList()
        };

        tv.NodeMouseClick += OnNodeMouseClick;
        tv.AfterSelect += OnAfterSelect;
        tv.BeforeLabelEdit += OnBeforeLabelEdit;
        tv.AfterLabelEdit += OnAfterLabelEdit;

        // ラベル編集終了後は LabelEdit を false に戻す
        tv.AfterLabelEdit += (_, _) => tv.LabelEdit = false;

        // ノード追加・展開後に横スクロール幅を自動調整する
        tv.AfterExpand  += (_, _) => AdjustTreeViewWidth(tv);
        tv.AfterCollapse += (_, _) => AdjustTreeViewWidth(tv);

        return tv;
    }

    /// <summary>
    /// TreeView の全ノードのテキスト幅を計測し、
    /// 横スクロールバーが出るよう ScrollBar の幅を調整する。
    /// </summary>
    private static void AdjustTreeViewWidth(TreeView tv)
    {
        var maxWidth = 0;

        using var g = tv.CreateGraphics();

        foreach (TreeNode node in tv.Nodes)
        {
            maxWidth = Math.Max(maxWidth, MeasureNodeWidth(g, tv, node));
            foreach (TreeNode child in node.Nodes)
                maxWidth = Math.Max(maxWidth, MeasureNodeWidth(g, tv, child));
        }

        // TreeView は内部的に horizontal scroll を自動管理するが、
        // SendMessage で HSCROLL を強制有効にする代わりに
        // 幅超過時は ScrollableControl として自動的にスクロールバーが出る。
        // ここでは ItemHeight ベースのパディングを加えた幅でダミーノードを制御するより、
        // ShowNodeToolTips で補完する方針とし、最低幅だけ保証する。
        if (maxWidth > tv.ClientSize.Width)
        {
            // ノードが収まらない場合はツールチップが表示される（ShowNodeToolTips=true）
            // 横スクロールバーを出すために ScrollBar を強制設定
            NativeMethods.SetTreeViewHorizontalScroll(tv.Handle);
        }
    }

    private static int MeasureNodeWidth(Graphics g, TreeView tv, TreeNode node)
    {
        // インデント幅 + アイコン幅 + テキスト幅 + 余白
        var indent = tv.Indent * node.Level;
        var iconWidth = tv.ImageList?.ImageSize.Width + 4 ?? 0;
        var textWidth = (int)g.MeasureString(node.Text, tv.Font).Width;
        return indent + iconWidth + textWidth + 16;
    }

    private static ImageList CreateImageList()
    {
        var list = new ImageList { ImageSize = new Size(16, 16) };

        // IconStage(0): フォルダアイコン（プレースホルダー）
        list.Images.Add(CreatePlaceholderIcon(Color.SteelBlue, "S"));

        // IconPage(1): シートアイコン（プレースホルダー）
        list.Images.Add(CreatePlaceholderIcon(Color.SeaGreen, "P"));

        return list;
    }

    /// <summary>
    /// アイコン画像が差し替えられるまでの仮アイコンを生成する。
    /// </summary>
    private static Bitmap CreatePlaceholderIcon(Color color, string letter)
    {
        var bmp = new Bitmap(16, 16);
        using var g = Graphics.FromImage(bmp);
        g.Clear(Color.Transparent);
        using var brush = new SolidBrush(color);
        g.FillRectangle(brush, 1, 1, 14, 14);
        using var font = new Font("Yu Gothic UI", 8, FontStyle.Bold, GraphicsUnit.Pixel);
        using var textBrush = new SolidBrush(Color.White);
        var size = g.MeasureString(letter, font);
        g.DrawString(letter, font, textBrush,
            (16 - size.Width) / 2,
            (16 - size.Height) / 2);
        return bmp;
    }

    private ContextMenuStrip CreateStageContextMenu()
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add("Rename", null, OnStageRename);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Clone",  null, OnStageClone);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Delete", null, OnStageDelete);
        return menu;
    }

    private ContextMenuStrip CreatePageContextMenu()
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add("Rename",    null, OnPageRename);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Duplicate", null, OnPageDuplicate);
         menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add((ToolStripItem)_pageBookmarkMenuItem);
       menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Delete",    null, OnPageDelete);
        return menu;
    }

    private void OnPageBookmarkToggle(object? sender, EventArgs e)
    {
        if (_pageMenu.Tag is not NodeTag tag || tag.Page == null) return;
        BookmarkToggleRequested?.Invoke(tag.Stage, tag.Page);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _currentPageFont?.Dispose();
        }

        base.Dispose(disposing);
    }

    //========================
    // 内部型定義
    //========================
    private enum NodeKind { Stage, Page }

    private sealed record NodeTag(NodeKind Kind, Stage Stage, Page? Page);

    /// <summary>
    /// TreeView の横スクロールバーを強制表示するための Win32 ヘルパー。
    /// </summary>
    private static class NativeMethods
    {
        private const int GWL_STYLE   = -16;
        private const int WS_HSCROLL  = 0x00100000;
        private const int TVS_NOHSCROLL = 0x8000;

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        /// <summary>
        /// TreeView の TVS_NOHSCROLL スタイルを除去して横スクロールを有効にする。
        /// </summary>
        public static void SetTreeViewHorizontalScroll(IntPtr handle)
        {
            var style = GetWindowLong(handle, GWL_STYLE);
            if ((style & TVS_NOHSCROLL) != 0)
            {
                SetWindowLong(handle, GWL_STYLE, style & ~TVS_NOHSCROLL);
            }
        }
    }
}
