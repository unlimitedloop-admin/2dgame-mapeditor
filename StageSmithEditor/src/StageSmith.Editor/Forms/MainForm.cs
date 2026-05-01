using StageSmith.Application.Commands;
using StageSmith.Core.Constants;
using StageSmith.Core.Models;
using StageSmith.Editor.Controls;
using StageSmith.Editor.Tools;

namespace StageSmith.Editor;

public partial class MainForm : Form
{
    //========================
    // EditorFile
    //========================
    private EditorProject? _project;
    private Stage? _stage;
    private Page? _page;
    private int _selectedTileId = -1;
    private Bitmap? _tileset;

    //========================
    // Managers
    //========================
    private readonly CommandManager _commandManager = new();
    private readonly ToolManager _toolManager = new();

    //========================
    // Controls
    //========================
    private readonly MapViewControl _mapView;
    private readonly TilePaletteControl _tilePalette;

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
        this.StartPosition = FormStartPosition.CenterScreen;
        this.KeyPreview = true;
        this.KeyDown += MainForm_KeyDown;

        _mapView = new MapViewControl
        {
            Location = ViewerConstants.MapViewLocation,
            Size = ViewerConstants.MapViewSize
        };
        Controls.Add(_mapView);

        _tilePalette = new TilePaletteControl
        {
            Location = ViewerConstants.TilePaletteLocation,
            Size = ViewerConstants.TilePaletteSize
        };
        Controls.Add(_tilePalette);

        InitializeTools();
        InitializeToolStrip();
        BindToolManager();

        this.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.ShiftKey)
            {
                _mapView.UpdateCursor();
            }
        };

        this.KeyUp += (_, e) =>
        {
            if (e.KeyCode == Keys.ShiftKey)
            {
                _mapView.UpdateCursor();
            }
        };

        // テスト用のダミーデータをロード
        LoadTest();

        // 初期状態のアイコンを設定
        UpdateTilePreviewIcon();
    }

    // REVIEW: 未使用?
    private void MainForm_Load(object sender, EventArgs e)
    {
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

        // MapView接続
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
    // ToolManager同期
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
    // Tool変更（Command経由）
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
