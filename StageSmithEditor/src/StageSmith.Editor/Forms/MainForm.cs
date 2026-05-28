using StageSmith.Application.Commands;
using StageSmith.Core.Constants;
using StageSmith.Core.Models;
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
    // Managers
    //========================
    private readonly CommandManager _commandManager = new();
    private readonly ToolManager _toolManager = new();

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

    //========================
    // Controls（DockContent 経由で参照）
    //========================
    private MapViewControl _mapView => _mapViewContent.MapView;
    private TilePaletteControl _tilePalette => _mapViewContent.TilePalette;
    private PropertyWindowControl _propertyWindow => _propertyWindowContent.PropertyWindow;
    private StageExplorerControl _stageExplorer => _stageExplorerContent.StageExplorer;

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
        };

        // VS2015 テーマを適用（インストール済みの場合）
        _dockPanel.Theme = new VS2015BlueTheme();

        Controls.Add(_dockPanel);

        // DockContent を生成
        _mapViewContent = new MapViewContent();
        _stageExplorerContent = new StageExplorerContent();
        _propertyWindowContent = new PropertyWindowContent();

        // クリック時のフォーカス設定
        _mapView.Click += (s, e) => _mapView.Focus();
        _tilePalette.Click += (s, e) => _tilePalette.Focus();
        Click += (s, e) => _mapView.Focus();

        InitializeTools();
        InitializeToolStrip();
        InitializeDockLayout();
        BindToolManager();
        BindTilePalette();

        // PropertyWindow のバインド
        _propertyWindow.Bind(_context);

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

        NewProject();

        UpdateTilePreviewIcon();
    }

    // REVIEW: 未使用?
    private void MainForm_Load(object sender, EventArgs e)
    {
    }

    // =========================
    // DockLayout 初期化
    // =========================
    private void InitializeDockLayout()
    {
        // 左: StageExplorer
        _stageExplorerContent.Show(_dockPanel, DockState.DockLeft);

        // 右: PropertyWindow
        _propertyWindowContent.Show(_dockPanel, DockState.DockRight);

        // 中央: MapView + TilePalette（Document 領域）
        _mapViewContent.Show(_dockPanel, DockState.Document);
    }

    // =========================
    // 初期化
    // =========================
    private void InitializeTools()
    {
        // Pen
        _penTool = new PenTool(
            () => _page?.TileMap,
            () => _selectedTileId,
            _commandManager,
            () => _mapView.Invalidate()
        );

        // Picker
        _pickerTool = new PickerTool(
            (x, y) => _page?.TileMap.GetTile(x, y) ?? -1,
            tileId =>
            {
                if (tileId < 0) return;

                _selectedTileId = tileId;
                _tilePalette.SetSelected(tileId);
                _mapView.PreviewTileId = tileId;
            }
        );

        // Selection
        _selectionTool = new SelectionTool(
            (x, y) =>
            {
                var map = _page?.TileMap;
                return (byte)(map == null ? 0 : map.GetTile(x, y));
            },
            () => (_page?.TileMap.Width ?? 16, _page?.TileMap.Height ?? 15)
        );

        _selectionTool.SelectionChanged += () => _mapView.Invalidate();
        _selectionTool.MoveRequested += OnSelectionMoveRequested;
        _selectionTool.Confirmed += ApplySelectionFill;

        // Fill
        _fillTool = new FillTool(
            () => _page?.TileMap,
            positions =>
            {
                if (_page == null || _selectedTileId < 0)
                    return;

                var command = new TilePaintCommand(
                    _page.TileMap,
                    positions,
                    (byte)_selectedTileId
                );

                _commandManager.Execute(command);
                _mapView.Invalidate();
            },
            _selectedTileId
        );

        // MapView接続（DockContent 生成後なので直接参照可能）
        _mapView.ToolManager = _toolManager;
        _mapView.PickerTool = _pickerTool;
        _mapView.SelectionTool = _selectionTool;
        _mapView.FillTool = _fillTool;

        _toolManager.SetTool(_penTool);

        // Drag Command
        _mapView.MouseDown += (s, e) =>
        {
            if (e.Button == MouseButtons.Left &&
                _page != null &&
                _currentMode == EditorToolMode.Pen)
            {
                _currentDragCommand = new DragPaintCommand(_page.TileMap);
            }
        };

        _mapView.MouseUp += (s, e) =>
        {
            if (_currentDragCommand != null && _currentDragCommand.HasChanges)
            {
                _commandManager.Execute(_currentDragCommand);
                _mapView.Invalidate();
            }

            _currentDragCommand = null;
        };
    }

    //========================
    // StageExplorer バインド
    //========================
    private void BindStageExplorer()
    {
        if (_context.Project == null) return;

        _stageExplorer.Bind(_context.Project);

        // ページ選択 → MapView に反映
        _stageExplorer.PageSelected += (stage, page) =>
        {
            _context.SetStage(_context.Project!.Stages.IndexOf(stage));
            _context.SetPage(stage.Pages.IndexOf(page));
            ApplyContextToView();
        };

        _stageExplorer.StageListChanged += () =>
        {
            _propertyWindow.RefreshProperties();
        };

        _stageExplorer.PageListChanged += _ =>
        {
            _propertyWindow.RefreshProperties();
        };

        SyncExplorerHighlight();
    }

    //========================
    // ToolManager 同期
    //========================
    private void BindToolManager()
    {
        _toolManager.ToolChanged += tool =>
        {
            if (ReferenceEquals(tool, _penTool))
                _currentMode = EditorToolMode.Pen;
            else if (ReferenceEquals(tool, _selectionTool))
                _currentMode = EditorToolMode.Selection;

            UpdateToolbarCheckedState();
            _mapView.Invalidate();
        };
    }

    //========================
    // Tool 変更（Command 経由）
    //========================
    private void ChangeTool(ITool? tool)
    {
        if (tool == null) return;

        _commandManager.Execute(
            new ChangeToolCommand(_toolManager, tool)
        );
    }

    //========================
    // モード変更（統一）
    //========================
    private void SetToolMode(EditorToolMode mode)
    {
        if (_currentMode == mode)
            return;

        ITool? tool = mode switch
        {
            EditorToolMode.Pen => _penTool,
            EditorToolMode.Selection => _selectionTool,
            _ => null
        };

        if (tool == null)
            return;

        ChangeTool(tool);

        if (mode == EditorToolMode.Pen)
            _selectionTool?.ClearSelection();
    }
}
