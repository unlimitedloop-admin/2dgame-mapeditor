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
        _commandManager.HistoryChanged += () => _mapView.Invalidate();

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
        if (!_stageExplorerContent.IsDisposed)
        {
            _stageExplorerContent.Show(_dockPanel, DockState.DockLeft);
        }

        if (!_metaTilePaletteContent.IsDisposed &&
            !_stageExplorerContent.IsDisposed &&
            _stageExplorerContent.Pane != null)
        {
            _metaTilePaletteContent.Show(
                _stageExplorerContent.Pane,
                DockAlignment.Bottom,
                0.35
            );
        }

        if (!_propertyWindowContent.IsDisposed)
        {
            _propertyWindowContent.Show(_dockPanel, DockState.DockRight);
        }

        if (!_mapViewContent.IsDisposed)
        {
            _mapViewContent.Show(_dockPanel, DockState.Document);
            _mapViewContent.UpdateZoomTitle(_mapView.ZoomScale);
        }
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

                // ノードエディタが開いていれば現在ページのプレビューを更新する
                if (_page != null)
                    _nodeEditorForm?.InvalidatePagePreview(_page.Id, _page);
            }

            _currentDragCommand = null;
        };
    }

    //========================
    // MetaTilePalette バインド
    //========================
    private void BindMetaTilePalette()
    {
        _metaTilePalette.MetaTileSelected += metaTile =>
        {
            _context.SetSelectedMetaTile(metaTile);

            // 通常タイル選択と競合しないように、見た目上の選択を解除する。
            _selectedTileId = -1;
            _tilePalette.SetSelected(-1);

            // 現段階では MapView 側の配置処理は次フェーズ。
            // プレビューも通常タイル用なので一旦消す。
            _mapView.PreviewTileId = -1;
            _mapView.ShowPreview = false;
            _mapView.Invalidate();
        };
    }

    //========================
    // PageNavBar バインド
    //========================
    private void BindPageNavigationController()
    {
        _pageNavigationController = new PageNavigationController(
            _context,
            _pageNavBar,
            _mapView
        );

        _context.ContextChanged += OnEditorContextChanged;

        _pageNavigationController.Refresh();
    }

    /// <summary>
    /// ページ移動を実行し、ビューを更新する。
    /// ナビゲーションバーのボタンとキーショートカットの両方から呼ばれる。
    /// </summary>
    public void NavigatePage(NavAction action)
    {
        _pageNavigationController?.Navigate(action);

        ApplyContextToView();
        _stageExplorer.RebuildTree();
        _pageNavBar.UpdateDisplay(_context);

        // ノードエディタが開いていれば選択も同期
        _nodeEditorForm?.SyncPageSelection(_context.CurrentPageIndex);
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

        // 現在表示中のページが削除されたとき → 直前 or 直後 or 空表示
        _stageExplorer.PageDeleted += (stage, nextPage) =>
        {
            var stageIndex = _context.Project!.Stages.IndexOf(stage);
            _context.SetStage(stageIndex);

            if (nextPage != null)
            {
                var pageIndex = stage.Pages.IndexOf(nextPage);
                _context.SetPage(pageIndex);
            }
            else
            {
                // ページが0件になった場合は空表示
                _page = null;
                _mapView.SetTileMap(null);
                _propertyWindow.RefreshProperties();
                _pageNavBar.UpdateDisplay(_context);
                _mapView.Invalidate();
                return;
            }

            ApplyContextToView();
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

    private void ShowDockContent(DockContent content, DockState dockState)
    {
        if (content.IsDisposed)
        {
            MessageBox.Show(
                $"{content.Text} は破棄されています。アプリを再起動してください。",
                "Window",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning
            );
            return;
        }

        if (content.DockPanel == null)
        {
            content.Show(_dockPanel, dockState);
        }
        else
        {
            content.Show();
            content.Activate();
        }
    }

    private void ShowMapViewContent()
    {
        ShowDockContent(_mapViewContent, DockState.Document);
        _mapViewContent.UpdateZoomTitle(_mapView.ZoomScale);
    }

    /// <summary>
    /// ノードエディタを開く。既に開いていれば前面に出す。
    /// </summary>
    private void OpenNodeEditor()
    {
        if (_nodeEditorForm == null || _nodeEditorForm.IsDisposed)
        {
            _nodeEditorForm = new PageNodeEditorForm(_context, _commandManager);

            _nodeEditorForm.PageSelected += pageIndex =>
            {
                _context.SetPage(pageIndex);
                ApplyContextToView();
                _pageNavBar.UpdateDisplay(_context);
            };

            _nodeEditorForm.Show(this);
        }
        else
        {
            _nodeEditorForm.BringToFront();
        }
    }

    /// <summary>
    /// メタタイルエディタを開く。既に開いていれば前面に出す。
    /// </summary>
    private void OpenMetaTileEditor()
    {
        var stage = _context.CurrentStage;

        if (stage == null)
        {
            MessageBox.Show(
                this,
                "ステージがありません。",
                "MetaTile Editor",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information
            );
            return;
        }

        if (_metaTileEditorForm == null || _metaTileEditorForm.IsDisposed)
        {
            _metaTileEditorForm = new MetaTileEditorForm(stage, _tileset);

            _metaTileEditorForm.MetaTilesChanged += RefreshMetaTilePaletteFromCurrentStage;

            _metaTileEditorForm.Show(this);
        }
        else
        {
            _metaTileEditorForm.BringToFront();
        }
    }

    private void ResetDockLayout()
    {
        _dockPanel.SuspendLayout(true);

        try
        {
            HideDockContent(_mapViewContent);
            HideDockContent(_stageExplorerContent);
            HideDockContent(_propertyWindowContent);
            HideDockContent(_metaTilePaletteContent);

            InitializeDockLayout();

            _mapViewContent.UpdateZoomTitle(_mapView.ZoomScale);
        }
        finally
        {
            _dockPanel.ResumeLayout(true, true);
        }
    }

    private static void HideDockContent(DockContent content)
    {
        if (content.IsDisposed)
            return;

        if (content.DockPanel != null)
            content.Hide();
    }
}
