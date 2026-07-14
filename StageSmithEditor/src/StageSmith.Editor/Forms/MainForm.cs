using StageSmith.Application.Commands;
using StageSmith.Core.Constants;
using StageSmith.Core.Models;
using StageSmith.Editor.Controllers;
using StageSmith.Editor.Controls;
using StageSmith.Editor.DockContents;
using StageSmith.Editor.Tools;
using WeifenLuo.WinFormsUI.Docking;

namespace StageSmith.Editor;

public partial class MainForm : Form
{
    //========================
    // EditorFile
    //========================
    private Page? _page;
    private int _selectedTileId = -1;
    private Bitmap? _tileset;
    private readonly EditorContext _context = new();

    //========================
    // Managers / Controllers
    //========================
    private readonly CommandManager _commandManager = new();
    private readonly ToolManager _toolManager = new();
    private PageNavigationController? _pageNavigationController;

    //========================
    // DockPanel
    //========================
    private readonly DockPanel _dockPanel;

    //========================
    // DockContents
    //========================
    private readonly MapViewContent _mapViewContent;
    private readonly StageExplorerContent _stageExplorerContent;
    private readonly PropertyWindowContent _propertyWindowContent;
    private readonly MetaTilePaletteContent _metaTilePaletteContent;

    //========================
    // EditorForms
    //========================
    private PageNodeEditorForm? _nodeEditorForm;
    private MetaTileEditorForm? _metaTileEditorForm;

    //========================
    // Controls（DockContent 経由で参照）
    //========================
    private MapViewControl _mapView => _mapViewContent.MapView;
    private TilePaletteControl _tilePalette => _mapViewContent.TilePalette;
    private PropertyWindowControl _propertyWindow => _propertyWindowContent.PropertyWindow;
    private StageExplorerControl _stageExplorer => _stageExplorerContent.StageExplorer;
    private MetaTilePaletteControl _metaTilePalette => _metaTilePaletteContent.MetaTilePalette;
    private PageNavBarControl _pageNavBar => _mapViewContent.PageNavBar;

    //========================
    // Tools
    //========================
    private PenTool? _penTool;
    private PickerTool? _pickerTool;
    private SelectionTool? _selectionTool;
    private FillTool? _fillTool;

    private DragPaintCommand? _currentDragCommand;
    private EditorToolMode _currentMode = EditorToolMode.Pen;

    //========================
    // その他
    //========================
    private bool _showGrid = true;
    private ClipboardData? _clipboard;

    //========================
    // 初期化
    //========================
    public MainForm()
    {
        InitializeComponent();
        StartPosition = FormStartPosition.CenterScreen;
        KeyPreview = true;

        // DockPanel をメインコンテナとして設置
        _dockPanel = new DockPanel
        {
            Dock = DockStyle.Fill,
            DocumentStyle = DocumentStyle.DockingWindow,
            // VS2015 テーマを適用（インストール済みの場合）
            Theme = new VS2015BlueTheme()
        };

        Controls.Add(_dockPanel);   // DockPanel をその下に配置
        InitializeToolStrip();
        Controls.Add(_menuStrip);   // メニューバーを最前面に

        // DockContent を生成
        _mapViewContent = new MapViewContent();
        _stageExplorerContent = new StageExplorerContent();
        _propertyWindowContent = new PropertyWindowContent();
        _metaTilePaletteContent = new MetaTilePaletteContent();

        _mapView.ZoomChanged += (_, _) =>
        {
            _mapViewContent.UpdateZoomTitle(_mapView.ZoomScale);
            RefreshViewMenuState();
        };

        // クリック時のフォーカス設定
        _mapView.Click += (s, e) => _mapView.Focus();
        _tilePalette.Click += (s, e) => _tilePalette.Focus();
        _metaTilePalette.Click += (s, e) => _metaTilePalette.Focus();
        Click += (s, e) => _mapView.Focus();

        InitializeMenuHandlers();

        InitializeTools();
        InitializeDockLayout();
        BindToolManager();
        BindTilePalette();
        BindMetaTilePalette();
        BindPageNavigationController();
        InitializeStageExplorerEvents();

        // PropertyWindow のバインド
        _propertyWindow.Bind(_context);

        // プロパティ変更 → エクスプローラー即時更新
        _propertyWindow.DataChanged += () =>
        {
            _stageExplorer.RebuildTree();
            SyncExplorerHighlight();
        };

        KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.ShiftKey)
                _mapView.UpdateCursor();
        };

        KeyUp += (_, e) =>
        {
            if (e.KeyCode == Keys.ShiftKey)
                _mapView.UpdateCursor();
        };

        // CommandManager の変更通知 → MapView を更新
        // ノードエディタ側でUndo/Redoが実行された場合もここで拾える
        _commandManager.HistoryChanged += () =>
        {
            _mapView.Invalidate();
            UpdateTitle();
        };

        InitializeSearch();

        FormClosing += (_, e) =>
        {
            if (!ConfirmDiscardChangesIfNeeded())
            {
                e.Cancel = true;
                return;
            }

            _nodeEditorForm?.Close();
            _metaTileEditorForm?.Close();
        };

        UpdateEditorAvailability();
        UpdateTitle();

        UpdateTilePreviewIcon();
    }

    // REVIEW: 未使用?
    private void MainForm_Load(object sender, EventArgs e)
    {
    }
}
