using StageSmith.Application.Commands;
using StageSmith.Core.Models;

namespace StageSmith.Editor;

public partial class MainForm : Form
{
    private readonly CommandManager _commandManager = new();

    private EditorProject? _project;
    private Stage? _stage;
    private Page? _page;
    private int _selectedTileId = -1;
    private Bitmap? _tileset;

    private bool _isMouseDown = false;
    private bool _showGrid = true;

    public MainForm()
    {
        InitializeComponent();
        this.KeyPreview = true;
        this.KeyDown += MainForm_KeyDown;
    }

    private void MainForm_Load(object sender, EventArgs e)
    {
        // テストデータ
        _project = new EditorProject();
        _stage = _project.AddStage("Stage 1");
        _stage.TilesetImagePath = @"Assets\DEMOSTAGE2_N0_ALL_PATTERN.png";
        _page = _stage.AddPage("Start");

        // タイルセット読み込み
        _tileset = new Bitmap(_stage.TilesetImagePath);

        var spacing = 2;
        var tileSize = 16;
        var tilesPerRow = _tileset.Width / tileSize;
        var tilesPerColumn = _tileset.Height / tileSize;
        var width = tilesPerRow * (tileSize + spacing);
        var height = tilesPerColumn * (tileSize + spacing);

        panelPalette.AutoScrollMinSize = new Size(width, height);
        panel1.Invalidate();
    }

    private void btnGrid_Click(object sender, EventArgs e)
    {
        _showGrid = !_showGrid;
        panel1.Invalidate();
    }
}
