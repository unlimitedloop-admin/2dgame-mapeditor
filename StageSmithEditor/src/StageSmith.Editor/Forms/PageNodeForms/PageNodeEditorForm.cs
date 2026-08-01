using StageSmith.Application.Commands;
using StageSmith.Core.Constants;
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
    private readonly EditorContext  _context;
    private readonly CommandManager _commandManager;

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
    private readonly ToolStripStatusLabel _zoomLabel;

    //========================
    // 情報表示領域のコントロール
    //========================
    private readonly Label _pageIndexLabel;
    private readonly Label _roomIdLabel;
    private readonly Label _tagsLabel;
    private readonly Label _remarksLabel;
    private readonly Label _connectionUpLabel;
    private readonly Label _connectionDownLabel;
    private readonly Label _connectionLeftLabel;
    private readonly Label _connectionRightLabel;
    private readonly Label _connectionBackLabel;
    private readonly Label _connectionFrontLabel;
    private readonly ComboBox _zComboBox;


    private NumberDisplayFormat _numberDisplayFormat = NumberDisplayFormat.Hex;

    public void SetNumberDisplayFormat(NumberDisplayFormat format)
    {
        _numberDisplayFormat = format;
        _nodeEditView.SetNumberDisplayFormat(format);
    }

    //========================
    // 初期化
    //========================
    public PageNodeEditorForm(EditorContext context, CommandManager commandManager)
    {
        _context        = context;
        _commandManager = commandManager;

        InitializeComponent();

        // ウィンドウ基本設定
        Text = "Page Node Editor";
        Size = new Size(800, 600);
        MinimumSize = new Size(500, 400);
        StartPosition = FormStartPosition.CenterScreen;
        KeyPreview    = true;

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
        _roomIdLabel    = CreateInfoLabel("Room ID: -", new Point(160, 4));
        _tagsLabel      = CreateInfoLabel("Tags: -", new Point(280, 4));
        _remarksLabel  = CreateInfoLabel("Bookmark: -", new Point(460, 4));

        // 接続情報ラベル群
        _connectionUpLabel      = CreateInfoLabel("Up:    ----", new Point(8, 28));
        _connectionDownLabel    = CreateInfoLabel("Down:  ----", new Point(200, 28));
        _connectionLeftLabel    = CreateInfoLabel("Left:  ----", new Point(8, 48));
        _connectionRightLabel   = CreateInfoLabel("Right: ----", new Point(200, 48));
        _connectionBackLabel    = CreateInfoLabel("Back:  ----", new Point(8, 68));
        _connectionFrontLabel   = CreateInfoLabel("Front: ----", new Point(200, 68));

        _infoPanel.Controls.AddRange([
            _zComboBox,
            _pageIndexLabel, _roomIdLabel, _tagsLabel, _remarksLabel,
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
        _nodeEditView.ZoomChanged     += zoom => _zoomLabel!.Text = $"x{zoom:0.00}";
        _nodeEditView.NodeMoved          += OnNodeMoved;
        _nodeEditView.NodeCopied         += OnNodeCopied;
        _nodeEditView.PasteModeConfirmed += OnPasteModeConfirmed;

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
            Spring    = true,  // 残りスペースを占有
        };
        _zoomLabel = new ToolStripStatusLabel("x1.00")
        {
            TextAlign = ContentAlignment.MiddleRight,
        };
        _statusStrip.Items.Add(_coordLabel);
        _statusStrip.Items.Add(_zoomLabel);

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

    /// <summary>
    /// タイルセット画像をノードビューに渡してプレビューキャッシュを再生成する。
    /// MainFormでタイルセットが変更されたとき呼び出す。
    /// </summary>
    public void SyncTileset(Bitmap? tileset)
    {
        var pages = _context.CurrentStage?.Pages;
        _nodeEditView.SetTileset(tileset, pages);
    }

    /// <summary>
    /// 特定ページのプレビューキャッシュを再生成する。
    /// ページのタイルを編集したあとにMainFormから呼び出す。
    /// </summary>
    public void InvalidatePagePreview(Guid pageId, Page page)
    {
        _nodeEditView.InvalidatePageCache(pageId, page);
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

        var h = page.Header;

        _pageIndexLabel.Text  = $"Page: {pageIndex}";
        _roomIdLabel.Text     = $"Room ID: 0x{h.RoomId:X2}";
        var tagLabels = page.TagIds
            .Select(idStr => Guid.TryParse(idStr, out var tagId) ? _context.Project?.FindTag(tagId) : null)
            .Where(t => t != null)
            .Select(t => t!.Label);

        _tagsLabel.Text = $"Tags: {(tagLabels.Any() ? string.Join(", ", tagLabels) : "-")}";
        _remarksLabel.Text   = $"Remarks: {(string.IsNullOrEmpty(page.Remarks) ? "-" : page.Remarks)}";

        _connectionUpLabel.Text    = FormatConnectionWithScroll("Up",    h.UpPage,    h.ScrollUp);
        _connectionDownLabel.Text  = FormatConnectionWithScroll("Down",  h.DownPage,  h.ScrollDown);
        _connectionLeftLabel.Text  = FormatConnectionWithScroll("Left",  h.LeftPage,  h.ScrollLeft);
        _connectionRightLabel.Text = FormatConnectionWithScroll("Right", h.RightPage, h.ScrollRight);
        _connectionBackLabel.Text  = FormatConnection("Back",  h.BackPage);
        _connectionFrontLabel.Text = FormatConnection("Front", h.FrontPage);
    }

    private void ClearInfoDisplay()
    {
        _pageIndexLabel.Text  = "Page: -";
        _roomIdLabel.Text     = "Room ID: -";
        _tagsLabel.Text       = "Tags: -";
        _remarksLabel.Text    = "Remarks: -";

        _connectionUpLabel.Text    = "Up:    ----";
        _connectionDownLabel.Text  = "Down:  ----";
        _connectionLeftLabel.Text  = "Left:  ----";
        _connectionRightLabel.Text = "Right: ----";
        _connectionBackLabel.Text  = "Back:  ----";
        _connectionFrontLabel.Text = "Front: ----";
    }

    /// <summary>Back/Front用：スクロール種類なし。</summary>
    private static string FormatConnection(string direction, byte roomId)
    {
        return roomId == 0xFF
            ? $"{direction,-6} ----"
            : $"{direction,-6} #{roomId}";
    }

    /// <summary>上下左右用：スクロール種類を併記する。</summary>
    private static string FormatConnectionWithScroll(string direction, byte roomId, byte scrollByte)
    {
        if (roomId == 0xFF)
            return $"{direction,-6} ----";

        var scrollType = ScrollEncoding.GetType(scrollByte);
        var scrollName = scrollType switch
        {
            ScrollType.Free    => "free",
            ScrollType.Page    => "page",
            ScrollType.Locked  => "locked",
            ScrollType.Axis    => "axis",
            ScrollType.Auto    => "auto",
            ScrollType.Object  => "object",
            ScrollType.Dynamic => "dynamic",
            _                  => "none",
        };

        return $"{direction,-6} #{roomId} / {scrollName}";
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
        // FilterZを更新してノードビューを再描画
        _nodeEditView.FilterZ = SelectedZ;
        _nodeEditView.Invalidate();

        // Z階層が変わったので選択状態と情報表示をリセット
        _nodeEditView.SetSelectedPage(-1);
        ClearInfoDisplay();
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
        var stage = _context.CurrentStage;
        if (stage == null) return;

        var page = stage.Pages.ElementAtOrDefault(pageIndex);
        if (page == null) return;

        using var dialog = new PageHeaderEditDialog(page, _numberDisplayFormat);
        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        var oldHeader  = page.Header;
        var oldEnable  = page.Enable;
        var oldRemarks = page.Remarks;

        var newHeader  = dialog.ResultHeader;
        var newEnable  = dialog.ResultEnable;
        var newRemarks = dialog.ResultRemarks;

        var noChange = oldHeader.ToBytes().SequenceEqual(newHeader.ToBytes())
                    && oldEnable == newEnable
                    && oldRemarks == newRemarks;

        if (!noChange)
        {
            _commandManager.Execute(new ActionCommand(
                () =>
                {
                    page.Header  = newHeader;
                    page.Enable  = newEnable;
                    page.Remarks = newRemarks;
                },
                () =>
                {
                    page.Header  = oldHeader;
                    page.Enable  = oldEnable;
                    page.Remarks = oldRemarks;
                }
            ));
        }

        _nodeEditView.Invalidate();
        UpdateInfoDisplay(pageIndex);
    }

    //========================
    // コンテキストメニュー
    //========================
    private ContextMenuStrip BuildContextMenu()
    {
        var menu = new ContextMenuStrip();

        // Enable Page 用メニュー項目
        var itemShowInView      = new ToolStripMenuItem("ページビューで表示（選択中にする）");
        var itemRemoveAssign    = new ToolStripMenuItem("部屋の割り当て削除");
        var itemToggleEnable    = new ToolStripMenuItem("ページの有効化／無効化");
        var itemAddUp           = new ToolStripMenuItem("上に新規ページを作成して接続");
        var itemAddDown         = new ToolStripMenuItem("下に新規ページを作成して接続");
        var itemAddLeft         = new ToolStripMenuItem("左に新規ページを作成して接続");
        var itemAddRight        = new ToolStripMenuItem("右に新規ページを作成して接続");
        var itemAddBack         = new ToolStripMenuItem("奥に新規ページを作成して接続");
        var itemAddFront        = new ToolStripMenuItem("手前に新規ページを作成して接続");
        var itemClearConnection = new ToolStripMenuItem("接続をクリア");
        var itemDuplicate       = new ToolStripMenuItem("このページを複製");
        var itemTemplate        = new ToolStripMenuItem("テンプレートから生成（将来拡張）") { Enabled = false };
        var itemConnectExisting = new ToolStripMenuItem("既存ページを隣接部屋として接続");

        // Disable Page 用メニュー項目
        var itemAssign = new ToolStripMenuItem("部屋の割り当て");

        itemShowInView.Click      += (_, _) => OnContextShowInView();
        itemRemoveAssign.Click    += (_, _) => OnContextRemoveAssign();
        itemToggleEnable.Click    += (_, _) => OnContextToggleEnable();
        itemAddUp.Click           += (_, _) => OnContextAddPage(Direction.Up);
        itemAddDown.Click         += (_, _) => OnContextAddPage(Direction.Down);
        itemAddLeft.Click         += (_, _) => OnContextAddPage(Direction.Left);
        itemAddRight.Click        += (_, _) => OnContextAddPage(Direction.Right);
        itemAddBack.Click         += (_, _) => OnContextAddPageZ(+1);
        itemAddFront.Click        += (_, _) => OnContextAddPageZ(-1);
        itemClearConnection.Click += (_, _) => OnContextClearConnection();
        itemDuplicate.Click       += (_, _) => OnContextDuplicate();
        itemConnectExisting.Click += (_, _) => OnContextConnectExisting();
        itemAssign.Click          += (_, _) => OnContextAssign();

        // インデックス:
        //  0: ページビューで表示
        //  1: Separator
        //  2: 割り当て削除
        //  3: 有効無効
        //  4: Separator
        //  5: 上に追加
        //  6: 下に追加
        //  7: 左に追加
        //  8: 右に追加
        //  9: 奥に追加
        // 10: 手前に追加
        // 11: Separator
        // 12: 接続クリア
        // 13: 複製
        // 14: 既存ページを隣接接続
        // 15: テンプレート
        // 16: Separator
        // 17: 部屋の割り当て
        menu.Items.AddRange([
            itemShowInView,
            new ToolStripSeparator(),
            itemRemoveAssign,
            itemToggleEnable,
            new ToolStripSeparator(),
            itemAddUp, itemAddDown, itemAddLeft, itemAddRight,
            itemAddBack, itemAddFront,
            new ToolStripSeparator(),
            itemClearConnection,
            itemDuplicate,
            itemConnectExisting,
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
        var hitPage      = _nodeEditView.HitTestPage();
        var hitCandidate = _nodeEditView.HitTestCandidateResult();

        var isEnablePage = hitPage != null;
        var isCandidate  = !isEnablePage && hitCandidate != null;

        // Enable Page 用の項目
        menu.Items[0].Visible  = isEnablePage;  // ページビューで表示
        menu.Items[1].Visible  = isEnablePage;  // Separator
        menu.Items[2].Visible  = isEnablePage;  // 割り当て削除
        menu.Items[3].Visible  = isEnablePage;  // 有効無効
        menu.Items[4].Visible  = isEnablePage;  // Separator
        menu.Items[5].Visible  = isEnablePage;  // 上に追加
        menu.Items[6].Visible  = isEnablePage;  // 下に追加
        menu.Items[7].Visible  = isEnablePage;  // 左に追加
        menu.Items[8].Visible  = isEnablePage;  // 右に追加
        menu.Items[9].Visible  = isEnablePage;  // 奥に追加
        menu.Items[10].Visible = isEnablePage;  // 手前に追加
        menu.Items[11].Visible = isEnablePage;  // Separator
        menu.Items[12].Visible = isEnablePage;  // 接続クリア
        menu.Items[13].Visible = isEnablePage;  // 複製
        menu.Items[14].Visible = isEnablePage;  // 既存ページを隣接接続
        menu.Items[15].Visible = isEnablePage;  // テンプレート
        menu.Items[16].Visible = isEnablePage;  // Separator

        // Disable Page 用の項目（候補位置のみ表示）
        menu.Items[17].Visible = isCandidate;   // 部屋の割り当て
    }

    //========================
    // コンテキストメニュー操作
    //========================

    // ① ページビューで表示
    private void OnContextShowInView()
    {
        var pageIndex = _nodeEditView.HitTestPage();
        if (pageIndex == null) return;

        _nodeEditView.SetSelectedPage(pageIndex.Value);
        UpdateInfoDisplay(pageIndex.Value);
        PageSelected?.Invoke(pageIndex.Value);
    }

    // ② 部屋の割り当て削除（接続クリア or ページ削除を選択）
    private void OnContextRemoveAssign()
    {
        var pageIndex = _nodeEditView.HitTestPage();
        if (pageIndex == null) return;

        var stage = _context.CurrentStage;
        if (stage == null) return;

        var page = stage.Pages.ElementAtOrDefault(pageIndex.Value);
        if (page == null) return;

        var result = MessageBox.Show(
            "接続情報のみクリアしますか？\n「いいえ」を選ぶとページ自体を削除します。",
            "部屋の割り当て削除",
            MessageBoxButtons.YesNoCancel,
            MessageBoxIcon.Question
        );

        if (result == DialogResult.Yes)
        {
            // 接続情報クリアのみ
            var command = new NodeClearConnectionCommand(page, () =>
            {
                _nodeEditView.Invalidate();
                UpdateInfoDisplay(pageIndex.Value);
            });
            _commandManager.Execute(command);
        }
        else if (result == DialogResult.No)
        {
            // ページ削除
            var command = new NodeRemovePageCommand(stage, page, () =>
            {
                _nodeEditView.SetSelectedPage(-1);
                ClearInfoDisplay();
                _nodeEditView.Invalidate();
            });
            _commandManager.Execute(command);
        }
    }

    // ③ ページの有効化／無効化
    private void OnContextToggleEnable()
    {
        var pageIndex = _nodeEditView.HitTestPage();
        if (pageIndex == null) return;

        var page = _context.CurrentStage?.Pages.ElementAtOrDefault(pageIndex.Value);
        if (page == null) return;

        page.Enable = !page.Enable;
        _nodeEditView.Invalidate();
        UpdateInfoDisplay(pageIndex.Value);
    }

    // ④ 上下左右に新規ページを作成して接続
    private void OnContextAddPage(Direction direction)
    {
        var pageIndex = _nodeEditView.HitTestPage();
        if (pageIndex == null) return;

        var stage = _context.CurrentStage;
        if (stage == null) return;

        var sourcePage = stage.Pages.ElementAtOrDefault(pageIndex.Value);
        if (sourcePage == null) return;

        var (dx, dy) = direction switch
        {
            Direction.Up    => (0, -1),
            Direction.Down  => (0,  1),
            Direction.Left  => (-1, 0),
            Direction.Right => (1,  0),
            _               => (0,  0),
        };

        var newX = sourcePage.NodeX + dx;
        var newY = sourcePage.NodeY + dy;

        if (stage.Pages.Any(p => p.NodeX == newX && p.NodeY == newY && p.Header.Z == FilterZ))
        {
            MessageBox.Show("その方向には既にページがあります。", "新規ページ作成",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var dirName = direction switch
        {
            Direction.Up    => "上",
            Direction.Down  => "下",
            Direction.Left  => "左",
            Direction.Right => "右",
            _               => "",
        };

        if (MessageBox.Show(
            $"ページ {pageIndex.Value} の{dirName}側に新規ページを増設します。よろしいですか？",
            "新規ページ作成", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            return;

        // 新規ページ生成
        var newPage = new Page
        {
            Name  = $"Page {stage.Pages.Count:D3}",
            NodeX = newX,
            NodeY = newY,
        };
        var newHeader = PageHeader.CreateDefault();
        newHeader.RoomId = GetNextAvailableRoomId(stage);
        newHeader.Z      = (byte)FilterZ;
        newPage.Header   = newHeader;

        // ★ Command生成前にsourceの現在ヘッダーを退避
        var sourceHeaderBefore = sourcePage.Header;

        // 双方向接続を計算（sourcePageのHeaderを一時変更）
        ConnectPages(sourcePage, newPage, direction);
        var sourceHeaderAfter = sourcePage.Header;

        // sourcePageを元に戻す（Commandに任せる）
        sourcePage.Header = sourceHeaderBefore;

        var command = new NodeAddPageCommand(stage, sourcePage, newPage, sourceHeaderAfter, () =>
        {
            var idx = stage.Pages.IndexOf(newPage);
            if (idx >= 0)
            {
                _nodeEditView.SetSelectedPage(idx);
                UpdateInfoDisplay(idx);
                PageSelected?.Invoke(idx);
            }
            _nodeEditView.Invalidate();
        });

        _commandManager.Execute(command);
    }

    /// <summary>
    /// 奥（dz=+1）または手前（dz=-1）に新規ページを作成して接続する。
    /// 新規ページは元ページと同じ[x,y]・Z±1に配置する。
    /// </summary>
    private void OnContextAddPageZ(int dz)
    {
        var pageIndex = _nodeEditView.HitTestPage();
        if (pageIndex == null) return;

        var stage = _context.CurrentStage;
        if (stage == null) return;

        var sourcePage = stage.Pages.ElementAtOrDefault(pageIndex.Value);
        if (sourcePage == null) return;

        var newZ = sourcePage.Header.Z + dz;

        if (newZ < 0 || newZ > 255)
        {
            MessageBox.Show("Z座標が範囲外です。", "新規ページ作成",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        if (stage.Pages.Any(p =>
            p.NodeX == sourcePage.NodeX && p.NodeY == sourcePage.NodeY && p.Header.Z == newZ))
        {
            MessageBox.Show("その方向には既にページがあります。", "新規ページ作成",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var dirName = dz > 0 ? "奥" : "手前";

        if (MessageBox.Show(
            $"ページ {pageIndex.Value} の{dirName}側に新規ページを増設します。よろしいですか？",
            "新規ページ作成", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            return;

        // 新規ページ生成
        var newPage = new Page
        {
            Name  = $"Page {stage.Pages.Count:D3}",
            NodeX = sourcePage.NodeX,
            NodeY = sourcePage.NodeY,
        };
        var newHeader = PageHeader.CreateDefault();
        newHeader.RoomId = GetNextAvailableRoomId(stage);
        newHeader.Z      = (byte)newZ;
        newPage.Header   = newHeader;

        // ★ Command生成前にsourceの現在ヘッダーを退避
        var sourceHeaderBefore = sourcePage.Header;

        // Back/Front接続を計算（sourcePageのHeaderを一時変更）
        var sh = sourcePage.Header;
        var th = newPage.Header;
        if (dz > 0) { sh.BackPage  = th.RoomId; th.FrontPage = sh.RoomId; }
        else        { sh.FrontPage = th.RoomId; th.BackPage  = sh.RoomId; }
        sourcePage.Header = sh;
        newPage.Header    = th;
        var sourceHeaderAfter = sourcePage.Header;

        // sourcePageを元に戻す（Commandに任せる）
        sourcePage.Header = sourceHeaderBefore;

        var capturedNewZ = newZ;
        var command = new NodeAddPageCommand(stage, sourcePage, newPage, sourceHeaderAfter, () =>
        {
            _nodeEditView.FilterZ = capturedNewZ;
            RefreshZComboBox();

            var idx = stage.Pages.IndexOf(newPage);
            if (idx >= 0)
            {
                _nodeEditView.SetSelectedPage(idx);
                UpdateInfoDisplay(idx);
                PageSelected?.Invoke(idx);
            }
            _nodeEditView.Invalidate();
        });

        _commandManager.Execute(command);
    }
    private void OnContextClearConnection()
    {
        var pageIndex = _nodeEditView.HitTestPage();
        if (pageIndex == null) return;

        var stage = _context.CurrentStage;
        if (stage == null) return;

        var page = stage.Pages.ElementAtOrDefault(pageIndex.Value);
        if (page == null) return;

        if (MessageBox.Show(
            "このページの接続情報をすべてクリアします。よろしいですか？",
            "接続をクリア",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question) != DialogResult.Yes) return;

        var command = new NodeClearConnectionCommand(page, () =>
        {
            _nodeEditView.Invalidate();
            UpdateInfoDisplay(pageIndex.Value);
        });
        _commandManager.Execute(command);
    }

    private void OnContextDuplicate()
    {
        var pageIndex = _nodeEditView.HitTestPage();
        if (pageIndex == null) return;

        // 複製モード開始
        _nodeEditView.StartPasteMode(pageIndex.Value);
        _coordLabel.Text = "複製先のノードを選択してください（Escでキャンセル）";
    }

    /// <summary>複製モードで貼り付け先が確定したとき呼ばれる。</summary>
    private void OnPasteModeConfirmed(int sourcePageIndex, int targetPageIndex, int newX, int newY)
    {
        _nodeEditView.CancelPasteMode();
        _coordLabel.Text = "";

        if (targetPageIndex >= 0)
            OverwritePage(sourcePageIndex, targetPageIndex);  // 既存ページへの上書き
        else
            OnNodeCopied(sourcePageIndex, newX, newY);        // 候補位置への新規追加
    }

    /// <summary>
    /// 複製元ページのタイルマップを複製先既存ページに上書きコピーする。
    /// NodeX/NodeY・接続情報・RoomIdは複製先のものを維持する。
    /// </summary>
    private void OverwritePage(int sourcePageIndex, int targetPageIndex)
    {
        var stage = _context.CurrentStage;
        if (stage == null) return;

        var sourcePage = stage.Pages.ElementAtOrDefault(sourcePageIndex);
        var targetPage = stage.Pages.ElementAtOrDefault(targetPageIndex);
        if (sourcePage == null || targetPage == null) return;

        // 上書き前のタイルマップを保存してUndoに対応
        var oldTileMap = targetPage.TileMap.Clone();
        var newTileMap = sourcePage.TileMap.Clone();

        var command = new TileMapOverwriteCommand(targetPage, oldTileMap, newTileMap, () =>
        {
            _nodeEditView.InvalidatePageCache(targetPage.Id, targetPage);
            _nodeEditView.SetSelectedPage(targetPageIndex);
            UpdateInfoDisplay(targetPageIndex);
            PageSelected?.Invoke(targetPageIndex);
            _nodeEditView.Invalidate();
        });

        _commandManager.Execute(command);
    }

    //========================
    // ノードD&Dハンドラ
    //========================

    /// <summary>Ctrl+ドラッグでノードを移動する。</summary>
    private void OnNodeMoved(int pageIndex, int newX, int newY)
    {
        var stage = _context.CurrentStage;
        if (stage == null) return;

        var page = stage.Pages.ElementAtOrDefault(pageIndex);
        if (page == null) return;

        // 移動先に既存ページがある場合はキャンセル
        var occupied = stage.Pages.Any(p =>
            p != page && p.NodeX == newX && p.NodeY == newY && p.Header.Z == FilterZ);

        if (occupied) return;

        var command = new NodeMoveCommand(page, newX, newY, () =>
        {
            _nodeEditView.Invalidate();
            UpdateInfoDisplay(pageIndex);
        });

        _commandManager.Execute(command);
    }

    /// <summary>Ctrl+Shift+ドラッグでノードを複製する。</summary>
    private void OnNodeCopied(int pageIndex, int newX, int newY)
    {
        var stage = _context.CurrentStage;
        if (stage == null) return;

        var sourcePage = stage.Pages.ElementAtOrDefault(pageIndex);
        if (sourcePage == null) return;

        // 複製先に既存ページがある場合はキャンセル
        var occupied = stage.Pages.Any(p =>
            p.NodeX == newX && p.NodeY == newY && p.Header.Z == FilterZ);

        if (occupied) return;

        // ページを複製（接続情報は引き継がない）
        var newPage   = sourcePage.Clone();
        newPage.NodeX = newX;
        newPage.NodeY = newY;

        var newHeader       = newPage.Header;
        newHeader.RoomId    = GetNextAvailableRoomId(stage);
        newHeader.LeftPage  = 0xFF;
        newHeader.RightPage = 0xFF;
        newHeader.UpPage    = 0xFF;
        newHeader.DownPage  = 0xFF;
        newHeader.BackPage  = 0xFF;
        newHeader.FrontPage = 0xFF;
        newPage.Header = newHeader;

        var command = new NodeCopyCommand(stage, newPage, () =>
        {
            // Execute後に選択・表示を更新
            var newIndex = stage.Pages.IndexOf(newPage);
            if (newIndex >= 0)
            {
                _nodeEditView.SetSelectedPage(newIndex);
                UpdateInfoDisplay(newIndex);
                PageSelected?.Invoke(newIndex);
            }
            _nodeEditView.Invalidate();
        });

        _commandManager.Execute(command);
    }

    private void OnContextConnectExisting()
    {
        var pageIndex = _nodeEditView.HitTestPage();
        if (pageIndex == null) return;

        var stage = _context.CurrentStage;
        if (stage == null) return;

        var sourcePage = stage.Pages.ElementAtOrDefault(pageIndex.Value);
        if (sourcePage == null) return;

        using var dialog = new ConnectExistingPageDialog(stage, sourcePage, pageIndex.Value, _numberDisplayFormat);

        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        var targetPage = stage.Pages.ElementAtOrDefault(dialog.SelectedTargetIndex);
        if (targetPage == null) return;

        // ConnectDirectionをConnectPagesに渡すためPageNodeEditorForm内のConnectPagesを呼ぶ
        var sh = sourcePage.Header;
        var th = targetPage.Header;

        switch (dialog.SelectedDirection)
        {
            case ConnectDirection.Right:
                sh.RightPage = th.RoomId; th.LeftPage  = sh.RoomId; break;
            case ConnectDirection.Left:
                sh.LeftPage  = th.RoomId; th.RightPage = sh.RoomId; break;
            case ConnectDirection.Down:
                sh.DownPage  = th.RoomId; th.UpPage    = sh.RoomId; break;
            case ConnectDirection.Up:
                sh.UpPage    = th.RoomId; th.DownPage  = sh.RoomId; break;
            case ConnectDirection.Back:
                sh.BackPage  = th.RoomId; th.FrontPage = sh.RoomId; break;
            case ConnectDirection.Front:
                sh.FrontPage = th.RoomId; th.BackPage  = sh.RoomId; break;
        }

        sourcePage.Header = sh;
        targetPage.Header = th;

        _nodeEditView.Invalidate();
        UpdateInfoDisplay(pageIndex.Value);
    }

    private void OnContextAssign()
    {
        var candidatePos = _nodeEditView.HitTestCandidateResult();
        if (candidatePos == null) return;

        var stage = _context.CurrentStage;
        if (stage == null) return;

        // 未配置ページ（同Z階層に配置されていないページ）を収集
        var placedPositions = stage.Pages
            .Where(p => p.Header.Z == FilterZ)
            .Select(p => new Point(p.NodeX, p.NodeY))
            .ToHashSet();

        // 未配置 = NodeX/NodeYがデフォルト(0,0)で他のページと座標が被っているページ
        // または、候補位置以外のどこにも配置されていないページ
        var unplacedPages = stage.Pages
            .Where(p => p.Header.Z != FilterZ ||
                        !placedPositions.Contains(new Point(p.NodeX, p.NodeY)) == false)
            .ToList();

        // シンプルに：全ページから候補位置を選んで割り当てる
        // 割り当てダイアログ（ページ一覧から選択）
        using var selectDialog = new AssignPageDialog(stage, FilterZ, candidatePos.Value);
        if (selectDialog.ShowDialog(this) != DialogResult.OK) return;

        var targetPage = stage.Pages.ElementAtOrDefault(selectDialog.SelectedPageIndex);
        if (targetPage == null) return;

        targetPage.NodeX    = candidatePos.Value.X;
        targetPage.NodeY    = candidatePos.Value.Y;

        var h   = targetPage.Header;
        h.Z     = (byte)FilterZ;
        targetPage.Header = h;

        _nodeEditView.SetSelectedPage(selectDialog.SelectedPageIndex);
        UpdateInfoDisplay(selectDialog.SelectedPageIndex);
        _nodeEditView.Invalidate();
    }

    //========================
    // 接続操作ヘルパー
    //========================

    /// <summary>
    /// source → new の方向に双方向接続を設定する。
    /// </summary>
    private static void ConnectPages(Page source, Page target, Direction direction)
    {
        var sh = source.Header;
        var th = target.Header;

        switch (direction)
        {
            case Direction.Right:
                sh.RightPage = th.RoomId;
                th.LeftPage  = sh.RoomId;
                break;
            case Direction.Left:
                sh.LeftPage  = th.RoomId;
                th.RightPage = sh.RoomId;
                break;
            case Direction.Down:
                sh.DownPage = th.RoomId;
                th.UpPage   = sh.RoomId;
                break;
            case Direction.Up:
                sh.UpPage   = th.RoomId;
                th.DownPage = sh.RoomId;
                break;
        }

        source.Header = sh;
        target.Header = th;
    }

    /// <summary>
    /// ステージ内で未使用の最小RoomIdを返す。
    /// </summary>
    private static byte GetNextAvailableRoomId(Stage stage)
    {
        var usedIds = stage.Pages
            .Select(p => p.Header.RoomId)
            .Where(id => id != 0xFF)
            .ToHashSet();

        for (byte id = 0; id < 0xFF; id++)
        {
            if (!usedIds.Contains(id))
                return id;
        }

        return 0xFF; // 満杯（254ページ上限）
    }

    private int FilterZ => _nodeEditView.FilterZ;

    //========================
    // キーボード操作
    //========================

    /// <summary>
    /// 矢印キーで隣接ページへ選択移動、PageUp/DownでZ階層を移動する。
    /// </summary>
    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        // Escapeキー → 複製モードキャンセル（通常時はスルー）
        if (keyData == Keys.Escape && _nodeEditView.IsPasteMode)
        {
            _nodeEditView.CancelPasteMode();
            _coordLabel.Text = "";
            return true;
        }

        if (keyData == (Keys.Control | Keys.A))
        {
            var focusedControl = FindFocusedControl(this);

            if (focusedControl is TextBoxBase textBox)
            {
                textBox.SelectAll();
                return true;
            }

            return true;
        }

        switch (keyData)
        {
            case Keys.Control | Keys.Z:
                _commandManager.Undo();
                _nodeEditView.Invalidate();
                return true;

            case Keys.Control | Keys.Y:
                _commandManager.Redo();
                _nodeEditView.Invalidate();
                return true;

            case Keys.Left:     MoveSelection(Direction.Left);  return true;
            case Keys.Right:    MoveSelection(Direction.Right); return true;
            case Keys.Up:       MoveSelection(Direction.Up);    return true;
            case Keys.Down:     MoveSelection(Direction.Down);  return true;
            case Keys.PageUp:   MoveSelectionZ(+1);             return true;
            case Keys.PageDown: MoveSelectionZ(-1);             return true;

            // 複製モード中のEnterキー → 現在選択中ノードに複製確定
            case Keys.Enter when _nodeEditView.IsPasteMode:
            {
                var current = _nodeEditView.SelectedPageIndex;
                if (current >= 0)
                {
                    var stage = _context.CurrentStage;
                    var page  = stage?.Pages.ElementAtOrDefault(current);
                    if (page != null)
                        // キーボード操作の場合は常に既存ページへの上書き
                        OnPasteModeConfirmed(
                            _nodeEditView.SelectedPageIndex,
                            current,
                            page.NodeX, page.NodeY);
                }
                return true;
            }
        }

        return base.ProcessCmdKey(ref msg, keyData);
    }

    /// <summary>
    /// 指定方向に隣接するページが存在すれば選択を移動する。
    /// 存在しない場合は何もしない。
    /// </summary>
    private void MoveSelection(Direction direction)
    {
        var stage = _context.CurrentStage;
        if (stage == null) return;

        var currentIndex = _nodeEditView.SelectedPageIndex;
        if (currentIndex < 0) return;

        var current = stage.Pages.ElementAtOrDefault(currentIndex);
        if (current == null) return;

        var (dx, dy) = direction switch
        {
            Direction.Left  => (-1,  0),
            Direction.Right => ( 1,  0),
            Direction.Up    => ( 0, -1),
            Direction.Down  => ( 0,  1),
            _               => ( 0,  0),
        };

        var targetIndex = stage.Pages.FindIndex(p =>
            p.NodeX    == current.NodeX + dx &&
            p.NodeY    == current.NodeY + dy &&
            p.Header.Z == FilterZ);

        if (targetIndex < 0) return;

        _nodeEditView.SetSelectedPage(targetIndex);
        UpdateInfoDisplay(targetIndex);
        PageSelected?.Invoke(targetIndex);
    }

    /// <summary>
    /// 現在選択中ページの同じ[x,y]でZ±1のページが存在すれば選択を移動する。
    /// 存在しない場合は何もしない。
    /// </summary>
    private void MoveSelectionZ(int dz)
    {
        var stage = _context.CurrentStage;
        if (stage == null) return;

        var currentIndex = _nodeEditView.SelectedPageIndex;
        if (currentIndex < 0) return;

        var current = stage.Pages.ElementAtOrDefault(currentIndex);
        if (current == null) return;

        var targetZ = current.Header.Z + dz;

        var targetIndex = stage.Pages.FindIndex(p =>
            p.NodeX    == current.NodeX &&
            p.NodeY    == current.NodeY &&
            p.Header.Z == targetZ);

        if (targetIndex < 0) return;

        // Z変更に合わせてコンボボックスとFilterZも更新
        _nodeEditView.FilterZ = targetZ;
        RefreshZComboBox();

        _nodeEditView.SetSelectedPage(targetIndex);
        UpdateInfoDisplay(targetIndex);
        PageSelected?.Invoke(targetIndex);
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

    private static Control? FindFocusedControl(Control parent)
    {
        foreach (Control control in parent.Controls)
        {
            if (control.Focused)
                return control;

            var focusedChild = FindFocusedControl(control);
            if (focusedChild is not null)
                return focusedChild;
        }

        return null;
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
