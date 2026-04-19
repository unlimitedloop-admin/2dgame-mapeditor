using StageSmith.Application.Commands;
using StageSmith.Core.Constants;
using StageSmith.Core.Models;
using StageSmith.Editor.Controls;
using StageSmith.Editor.Tools;

namespace StageSmith.Editor;

public partial class MainForm : Form
{
    private readonly CommandManager _commandManager = new();

    private readonly MapViewControl _mapView;
    private readonly TilePaletteControl _tilePalette;

    private EditorProject? _project;
    private Stage? _stage;
    private Page? _page;
    private int _selectedTileId = -1;
    private Bitmap? _tileset;

    private bool _showGrid = true;
    private bool _showPreview = false;

    private EditorToolMode _currentMode = EditorToolMode.Pen;

    private ITool? _penTool;
    private ITool? _pickerTool;
    private ITool? _selectionTool;

    private DragPaintCommand? _currentDragCommand;

    private byte[,]? _clipboardTiles;
    private int _clipboardWidth;
    private int _clipboardHeight;

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

        // テスト用のダミーデータをロード
        LoadTest();

        // 初期状態のアイコンを設定
        UpdateTilePreviewIcon();
    }

    private void MainForm_Load(object sender, EventArgs e)
    {
    }
}
