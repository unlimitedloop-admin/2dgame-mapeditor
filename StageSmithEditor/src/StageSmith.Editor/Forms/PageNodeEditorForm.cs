using StageSmith.Core.Models;

namespace StageSmith.Editor;

/// <summary>
/// ステージのページ接続関係を視覚的に編集するノードエディタウィンドウ。
/// MainFormから開く独立ウィンドウとして動作する。
/// データの主権はMainFormのEditorContextが持つ。
/// </summary>
public partial class PageNodeEditorForm : Form
{
    //========================
    // データ参照
    //========================

    /// <summary>
    /// MainFormが持つEditorContextへの参照。
    /// ノードエディタはこれを通じてデータを読み書きする。
    /// </summary>
    private readonly EditorContext _context;

    //========================
    // イベント
    //========================

    /// <summary>
    /// ノードエディタ側でページが選択されたとき発火する。
    /// MainFormのマップビューがこれを受けて該当ページを表示する。
    /// </summary>
    public event Action<int>? PageSelected;

    //========================
    // Controls
    //========================

    /// <summary>上部：選択ページの情報表示領域</summary>
    private readonly Panel _infoPanel;

    /// <summary>中央：ノード編集ビュー（カスタム描画）</summary>
    private readonly NodeEditView _nodeEditView;

    /// <summary>ノードビュー右側の垂直スクロールバー</summary>
    private readonly VScrollBar _vScrollBar;

    /// <summary>ノードビュー下側の水平スクロールバー</summary>
    private readonly HScrollBar _hScrollBar;

    /// <summary>下部：ホバー座標表示ステータスバー</summary>
    private readonly StatusStrip _statusStrip;
    private readonly ToolStripStatusLabel _coordLabel;

    //========================
    // 情報表示領域のコントロール
    //========================
    private readonly Label _pageIndexLabel;
    private readonly Label _roomIdLabel;
    private readonly Label _tagsLabel;
    private readonly Label _bookmarkLabel;
    private readonly Label _connectionUpLabel;
    private readonly Label _connectionDownLabel;
    private readonly Label _connectionLeftLabel;
    private readonly Label _connectionRightLabel;
    private readonly Label _connectionBackLabel;
    private readonly Label _connectionFrontLabel;
    private readonly ComboBox _zComboBox;

    //========================
    // 初期化
    //========================
    public PageNodeEditorForm(EditorContext context)
    {
        _context = context;

        InitializeComponent();

        // ウィンドウ基本設定
        Text = "Page Node Editor";
        Size = new Size(800, 600);
        MinimumSize = new Size(500, 400);
        StartPosition = FormStartPosition.Manual;

        // メインレイアウト（上：情報表示、中：ノードビュー、下：ステータスバー）
        var mainLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 3,
            ColumnCount = 1,
        };
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 120));  // 情報表示領域
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));   // ノードビュー
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));   // ステータスバー

        // --- 情報表示領域 ---
        _infoPanel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = SystemColors.ControlLight,
            Padding = new Padding(8, 4, 8, 4),
        };

        // Z座標選択ドロップダウン
        _zComboBox = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Width = 60,
            Location = new Point(8, 4),
        };
        _zComboBox.SelectedIndexChanged += OnZComboBoxChanged;

        // ページ情報ラベル群
        _pageIndexLabel = CreateInfoLabel("Page: -", new Point(80, 4));
        _roomIdLabel = CreateInfoLabel("Room ID: -", new Point(160, 4));
        _tagsLabel = CreateInfoLabel("Tags: -", new Point(280, 4));
        _bookmarkLabel = CreateInfoLabel("Bookmark: -", new Point(460, 4));

        // 接続情報ラベル群
        _connectionUpLabel = CreateInfoLabel("Up:    ----", new Point(8, 28));
        _connectionDownLabel = CreateInfoLabel("Down:  ----", new Point(200, 28));
        _connectionLeftLabel = CreateInfoLabel("Left:  ----", new Point(8, 48));
        _connectionRightLabel = CreateInfoLabel("Right: ----", new Point(200, 48));
        _connectionBackLabel = CreateInfoLabel("Back:  ----", new Point(8, 68));
        _connectionFrontLabel = CreateInfoLabel("Front: ----", new Point(200, 68));

        _infoPanel.Controls.AddRange([
            _zComboBox,
            _pageIndexLabel, _roomIdLabel, _tagsLabel, _bookmarkLabel,
            _connectionUpLabel, _connectionDownLabel,
            _connectionLeftLabel, _connectionRightLabel,
            _connectionBackLabel, _connectionFrontLabel,
        ]);

        // --- ノード編集ビュー + スクロールバー ---
        // NodeEditViewとスクロールバーをPanelで組み合わせる
        //
        //  ┌──────────────┬───┐
        //  │ NodeEditView  │ V │
        //  ├──────────────┼───┤
        //  │ HScrollBar   │   │
        //  └──────────────┴───┘

        _nodeEditView = new NodeEditView(_context)
        {
            Dock = DockStyle.Fill,
        };

        _vScrollBar = new VScrollBar
        {
            Dock    = DockStyle.Right,
            Minimum = -5000,
            Maximum = 5000,
            Value   = 0,
        };

        _hScrollBar = new HScrollBar
        {
            Dock    = DockStyle.Bottom,
            Minimum = -5000,
            Maximum = 5000,
            Value   = 0,
        };

        // スクロールバー操作 → ViewOffsetを更新
        _vScrollBar.Scroll += (_, _) =>
        {
            var offset = _nodeEditView.ViewOffset;
            offset.Y = -_vScrollBar.Value;
            _nodeEditView.ViewOffset = offset;
        };

        _hScrollBar.Scroll += (_, _) =>
        {
            var offset = _nodeEditView.ViewOffset;
            offset.X = -_hScrollBar.Value;
            _nodeEditView.ViewOffset = offset;
        };

        // ドラッグ・ホイール操作 → スクロールバーを同期
        _nodeEditView.ViewOffsetChanged += offset =>
        {
            _hScrollBar.Value = Math.Clamp((int)-offset.X, _hScrollBar.Minimum, _hScrollBar.Maximum);
            _vScrollBar.Value = Math.Clamp((int)-offset.Y, _vScrollBar.Minimum, _vScrollBar.Maximum);
        };

        _nodeEditView.PageSelected    += OnNodeViewPageSelected;
        _nodeEditView.CoordChanged    += OnNodeViewCoordChanged;
        _nodeEditView.PageDoubleClick += OnNodeViewPageDoubleClick;

        // ノードビューエリア（NodeEditView + スクロールバー）
        var nodeViewArea = new Panel { Dock = DockStyle.Fill };
        nodeViewArea.Controls.Add(_nodeEditView);
        nodeViewArea.Controls.Add(_vScrollBar);
        nodeViewArea.Controls.Add(_hScrollBar);

        // --- ステータスバー ---
        _statusStrip = new StatusStrip { SizingGrip = false };
        _coordLabel = new ToolStripStatusLabel("x: -, y: -")
        {
            TextAlign = ContentAlignment.MiddleLeft,
        };
        _statusStrip.Items.Add(_coordLabel);

        // レイアウトに追加
        mainLayout.Controls.Add(_infoPanel,    0, 0);
        mainLayout.Controls.Add(nodeViewArea,  0, 1);
        mainLayout.Controls.Add(_statusStrip,  0, 2);

        Controls.Add(mainLayout);

        // コンテキストメニュー
        _nodeEditView.ContextMenuStrip = BuildContextMenu();

        // 初期表示
        RefreshZComboBox();
        ClearInfoDisplay();

        // ウィンドウ表示後にノードを中央配置
        Shown += (_, _) => _nodeEditView.CenterView();
    }

    //========================
    // 公開メソッド
    //========================

    /// <summary>
    /// MainFormからページ切り替えが通知されたとき呼ばれる。
    /// ノードビューの選択状態をMainFormに追従させる。
    /// </summary>
    public void SyncPageSelection(int pageIndex)
    {
        _nodeEditView.SetSelectedPage(pageIndex);
        UpdateInfoDisplay(pageIndex);
    }

    //========================
    // 情報表示領域
    //========================
    private void UpdateInfoDisplay(int pageIndex)
    {
        var page = _context.CurrentStage?.Pages.ElementAtOrDefault(pageIndex);
        if (page == null)
        {
            ClearInfoDisplay();
            return;
        }

        var header = page.Header;

        _pageIndexLabel.Text = $"Page: {pageIndex}";
        _roomIdLabel.Text = $"Room ID: 0x{header.RoomId:X2}";
        _tagsLabel.Text = $"Tags: {(string.IsNullOrEmpty(page.Tag) ? "-" : page.Tag)}";
        _bookmarkLabel.Text = $"Bookmark: {(string.IsNullOrEmpty(page.Remarks) ? "-" : page.Remarks)}";

        _connectionUpLabel.Text = FormatConnection("Up", header.UpPage);
        _connectionDownLabel.Text = FormatConnection("Down", header.DownPage);
        _connectionLeftLabel.Text = FormatConnection("Left", header.LeftPage);
        _connectionRightLabel.Text = FormatConnection("Right", header.RightPage);
        _connectionBackLabel.Text = FormatConnection("Back", header.BackPage);
        _connectionFrontLabel.Text = FormatConnection("Front", header.FrontPage);
    }

    private void ClearInfoDisplay()
    {
        _pageIndexLabel.Text = "Page: -";
        _roomIdLabel.Text = "Room ID: -";
        _tagsLabel.Text = "Tags: -";
        _bookmarkLabel.Text = "Bookmark: -";

        _connectionUpLabel.Text = "Up:    ----";
        _connectionDownLabel.Text = "Down:  ----";
        _connectionLeftLabel.Text = "Left:  ----";
        _connectionRightLabel.Text = "Right: ----";
        _connectionBackLabel.Text = "Back:  ----";
        _connectionFrontLabel.Text = "Front: ----";
    }

    private static string FormatConnection(string direction, byte roomId)
    {
        return roomId == 0xFF
            ? $"{direction,-6} ----"
            : $"{direction,-6} #{roomId}";
    }

    //========================
    // Z座標コンボボックス
    //========================
    private void RefreshZComboBox()
    {
        _zComboBox.Items.Clear();

        var stage = _context.CurrentStage;
        if (stage == null) return;

        // 使用中のZ値を収集してドロップダウンに追加
        var zValues = stage.Pages
            .Select(p => p.Header.Z)
            .Distinct()
            .OrderBy(z => z)
            .ToList();

        foreach (var z in zValues)
            _zComboBox.Items.Add(z);

        if (_zComboBox.Items.Count > 0)
            _zComboBox.SelectedIndex = 0;
    }

    private void OnZComboBoxChanged(object? sender, EventArgs e)
    {
        _nodeEditView.Invalidate();
    }

    public int SelectedZ =>
        _zComboBox.SelectedItem is byte z ? z : 0;

    //========================
    // ノードビューイベント
    //========================
    private void OnNodeViewPageSelected(int pageIndex)
    {
        UpdateInfoDisplay(pageIndex);

        // MainFormのマップビューに通知
        PageSelected?.Invoke(pageIndex);
    }

    private void OnNodeViewCoordChanged(int x, int y)
    {
        _coordLabel.Text = $"x: {x}, y: {y}";
    }

    private void OnNodeViewPageDoubleClick(int pageIndex)
    {
        // ページヘッダー編集ダイアログを開く（後続タスク）
        // TODO: PageHeaderEditDialog を実装後に接続する
        MessageBox.Show($"ページ {pageIndex} の編集ダイアログ（未実装）", "Page Header Edit");
    }

    //========================
    // コンテキストメニュー
    //========================
    private ContextMenuStrip BuildContextMenu()
    {
        var menu = new ContextMenuStrip();

        // Enable Page 用メニュー項目
        var itemShowInView = new ToolStripMenuItem("ページビューで表示（選択中にする）");
        var itemRemoveAssign = new ToolStripMenuItem("部屋の割り当て削除");
        var itemToggleEnable = new ToolStripMenuItem("ページの有効化／無効化");
        var itemAddUp = new ToolStripMenuItem("上に新規ページを作成して接続");
        var itemAddDown = new ToolStripMenuItem("下に新規ページを作成して接続");
        var itemAddLeft = new ToolStripMenuItem("左に新規ページを作成して接続");
        var itemAddRight = new ToolStripMenuItem("右に新規ページを作成して接続");
        var itemClearConnection = new ToolStripMenuItem("接続をクリア");
        var itemDuplicate = new ToolStripMenuItem("このページを複製");
        var itemTemplate = new ToolStripMenuItem("テンプレートから生成（将来拡張）") { Enabled = false };

        // Disable Page 用メニュー項目
        var itemAssign = new ToolStripMenuItem("部屋の割り当て");

        itemShowInView.Click += (_, _) => OnContextShowInView();
        itemRemoveAssign.Click += (_, _) => OnContextRemoveAssign();
        itemToggleEnable.Click += (_, _) => OnContextToggleEnable();
        itemAddUp.Click += (_, _) => OnContextAddPage(Direction.Up);
        itemAddDown.Click += (_, _) => OnContextAddPage(Direction.Down);
        itemAddLeft.Click += (_, _) => OnContextAddPage(Direction.Left);
        itemAddRight.Click += (_, _) => OnContextAddPage(Direction.Right);
        itemClearConnection.Click += (_, _) => OnContextClearConnection();
        itemDuplicate.Click += (_, _) => OnContextDuplicate();
        itemAssign.Click += (_, _) => OnContextAssign();

        menu.Items.AddRange([
            itemShowInView,
            new ToolStripSeparator(),
            itemRemoveAssign,
            itemToggleEnable,
            new ToolStripSeparator(),
            itemAddUp, itemAddDown, itemAddLeft, itemAddRight,
            new ToolStripSeparator(),
            itemClearConnection,
            itemDuplicate,
            itemTemplate,
            new ToolStripSeparator(),
            itemAssign,
        ]);

        // 右クリック時にノードの状態に合わせてメニューを切り替える
        menu.Opening += (_, _) => RefreshContextMenuState(menu);

        return menu;
    }

    private void RefreshContextMenuState(ContextMenuStrip menu)
    {
        var hitPage = _nodeEditView.HitTestPage();
        var isEnablePage = hitPage != null;

        // Enable Page 用の項目
        menu.Items[0].Visible = isEnablePage;  // ページビューで表示
        menu.Items[1].Visible = isEnablePage;  // Separator
        menu.Items[2].Visible = isEnablePage;  // 割り当て削除
        menu.Items[3].Visible = isEnablePage;  // 有効無効
        menu.Items[4].Visible = isEnablePage;  // Separator
        menu.Items[5].Visible = isEnablePage;  // 上に追加
        menu.Items[6].Visible = isEnablePage;  // 下に追加
        menu.Items[7].Visible = isEnablePage;  // 左に追加
        menu.Items[8].Visible = isEnablePage;  // 右に追加
        menu.Items[9].Visible = isEnablePage;  // Separator
        menu.Items[10].Visible = isEnablePage;  // 接続クリア
        menu.Items[11].Visible = isEnablePage;  // 複製
        menu.Items[12].Visible = isEnablePage;  // テンプレート
        menu.Items[13].Visible = isEnablePage;  // Separator

        // Disable Page 用の項目（候補位置）
        menu.Items[14].Visible = !isEnablePage; // 部屋の割り当て
    }

    //========================
    // コンテキストメニュー操作（スタブ）
    // ※ 各操作の実装は後続タスクで追加する
    //========================
    private void OnContextShowInView()
    {
        var pageIndex = _nodeEditView.SelectedPageIndex;
        if (pageIndex < 0) return;
        PageSelected?.Invoke(pageIndex);
    }

    private void OnContextRemoveAssign()
    {
        // TODO: 部屋割り当て削除の実装
    }

    private void OnContextToggleEnable()
    {
        // TODO: 有効無効切り替えの実装
    }

    private void OnContextAddPage(Direction direction)
    {
        // TODO: 新規ページ作成＋接続の実装
    }

    private void OnContextClearConnection()
    {
        // TODO: 接続クリアの実装
    }

    private void OnContextDuplicate()
    {
        // TODO: ページ複製の実装
    }

    private void OnContextAssign()
    {
        // TODO: 部屋割り当ての実装
    }

    //========================
    // ヘルパー
    //========================
    private static Label CreateInfoLabel(string text, Point location)
    {
        return new Label
        {
            Text = text,
            Location = location,
            AutoSize = true,
            Font = new Font("Yu Gothic UI", 9f),
        };
    }
}

/// <summary>方向の定義。コンテキストメニューの新規ページ追加方向に使用。</summary>
public enum Direction
{
    Up,
    Down,
    Left,
    Right,
}
