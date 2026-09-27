using StageSmith.Application.Commands;
using StageSmith.Core.Constants;
using StageSmith.Core.Models;
using StageSmith.Editor.Controllers;
using StageSmith.Editor.Controls;
using StageSmith.Editor.DockContents;
using StageSmith.Editor.Forms;
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
    private Guid? _loadedTilesetStageId;
    private Guid? _lastAppliedPageId;       // ページ移動検知用
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
    private readonly BookmarkListContent _bookmarkListContent;
    private readonly MarkerColorPanelContent _markerColorPanelContent;
    private readonly ObjectPaletteContent _objectPaletteContent;

    //========================
    // EditorForms
    //========================
    private PageNodeEditorForm? _nodeEditorForm;
    private MetaTileEditorForm? _metaTileEditorForm;
    private StageMapViewerForm? _stageMapViewerForm;
    private TagManagerForm? _tagManagerForm;

    //========================
    // Controls（DockContent 経由で参照）
    //========================
    private MapViewControl _mapView => _mapViewContent.MapView;
    private TilePaletteControl _tilePalette => _mapViewContent.TilePalette;
    private PropertyWindowControl _propertyWindow => _propertyWindowContent.PropertyWindow;
    private StageExplorerControl _stageExplorer => _stageExplorerContent.StageExplorer;
    private MetaTilePaletteControl _metaTilePalette => _metaTilePaletteContent.MetaTilePalette;
    private PageNavBarControl _pageNavBar => _mapViewContent.PageNavBar;
    private BookmarkListControl _bookmarkList => _bookmarkListContent.BookmarkList;
    private MarkerColorPanelControl _markerColorPanel => _markerColorPanelContent.MarkerColorPanel;
    private ObjectPaletteControl _objectPalette => _objectPaletteContent.ObjectPalette;

    //========================
    // Tools
    //========================
    private PenTool? _penTool;
    private PickerTool? _pickerTool;
    private SelectionTool? _selectionTool;
    private FillTool? _fillTool;
    private MarkerTool? _markerTool;

    private DragPaintCommand? _currentDragCommand;
    private EditorToolMode _currentMode = EditorToolMode.Pen;

    //========================
    // その他
    //========================
    private bool _showGrid = true;
    private ClipboardData? _clipboard;
    private readonly MarkerState _markerState = new();

    //========================
    // 初期化
    //========================
    public MainForm()
    {
        InitializeComponent();
        LoadConfig();   // ini読込 + ウィンドウ位置復元（未保存時はCenterScreenへフォールバック）
        KeyPreview = true;

        // DockPanel をメインコンテナとして設置
        _dockPanel = new DockPanel
        {
            Dock = DockStyle.Fill,
            DocumentStyle = DocumentStyle.DockingWindow,
            // VS2015 テーマを適用（インストール済みの場合）
            Theme = new VS2015BlueTheme()
        };

        Controls.Add(_dockPanel);
        InitializeToolStrip();
        InitializeStatusBar();
        Controls.Add(_menuStrip);

        // DockContent を生成
        _mapViewContent = new MapViewContent();
        _stageExplorerContent = new StageExplorerContent();
        _propertyWindowContent = new PropertyWindowContent();
        _metaTilePaletteContent = new MetaTilePaletteContent();
        _bookmarkListContent = new BookmarkListContent();
        _markerColorPanelContent = new MarkerColorPanelContent();
        _objectPaletteContent = new ObjectPaletteContent();

        _mapView.ZoomChanged += (_, _) =>
        {
            _mapViewContent.UpdateZoomTitle(_mapView.ZoomScale);
            RefreshViewMenuState();
        };

        _mapView.ContextMenuRequested += OnMapViewContextMenuRequested;

        // クリック時のフォーカス設定
        _mapView.Click += (s, e) => _mapViewContent.FocusMapView();
        _tilePalette.Click += (s, e) => _tilePalette.Focus();
        _metaTilePalette.Click += (s, e) => _metaTilePalette.Focus();
        Click += (s, e) => _mapView.Focus();

        InitializeMenuHandlers();

        InitializeTools();

        BindToolManager();
        ApplyToolModeFromConfig(); // ツール初期化・イベント購読後に復元

        BindTilePalette();
        BindMetaTilePalette();
        BindObjectPalette();
        BindMarkerColorPanel();
        BindPageNavigationController();
        
        InitializeStageExplorerEvents();
        InitializeBookmarkEvents();
        
        BindBookmarkList();

        // PropertyWindow のバインド
        _propertyWindow.Bind(_context);
        _propertyWindow.CommandRequested += cmd => _commandManager.Execute(cmd);

        _propertyWindow.TreeRelevantDataChanged += () =>
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

        _commandManager.HistoryChanged += () =>
        {
            _context.CurrentStage?.MarkDirty();

            _undoButton.Enabled = _commandManager.CanUndo && !_commandManager.IsReadOnly;
            _redoButton.Enabled = _commandManager.CanRedo && !_commandManager.IsReadOnly;

            // NOTE: このイベントはコントロールのLeave等、フォーカス遷移の"最中"に
            // 同期的に発火することがある。ここでTreeViewの再構築やコンボの値再代入を
            // 同期的に行うと、遷移元/遷移先コントロールが自分の入力処理中に
            // 横から状態を書き換えられる形になり、WinForms側のメッセージ処理と
            // 競合してハング・描画崩壊を起こす。
            // → 現在のメッセージ処理が完全に終わった後に回すことで回避する。
            BeginInvoke(() =>
            {
                _mapView.Invalidate();
                UpdateTitle();
                _stageExplorer?.RebuildTree();
                _bookmarkList?.RefreshList();
                _pageNavBar?.UpdateDisplay(_context);
                _propertyWindow?.RefreshProperties();
                _nodeEditorForm?.SyncPageSelection(_context.CurrentPageIndex);
            });
        };

        InitializeSearch();

        FormClosing += (_, e) =>
        {
            if (!ConfirmDiscardChangesIfNeeded())
            {
                e.Cancel = true;
                return;
            }

            SaveConfig();
            SaveDockLayout();

            _nodeEditorForm?.Close();
            _metaTileEditorForm?.Close();
            _stageMapViewerForm?.Close();
        };

        UpdateEditorAvailability();
        UpdateTitle();

        ApplyViewStateFromConfig();
        ApplyNumberDisplayFormat();
    }

    private void MainForm_Load(object sender, EventArgs e)
    {
        InitializeDockLayoutFromSavedOrDefault();
    }
}
