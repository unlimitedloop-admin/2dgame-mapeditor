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

        LoadTest();
    }

    private void MainForm_Load(object sender, EventArgs e)
    {
    }
}
