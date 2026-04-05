using StageSmith.Application.Commands;
using StageSmith.Core.Constants;
using StageSmith.Core.Models;
using StageSmith.Editor.Controls;

namespace StageSmith.Editor;

public partial class MainForm : Form
{
    private readonly CommandManager _commandManager = new();

    private readonly MapViewControl _mapView;

    private EditorProject? _project;
    private Stage? _stage;
    private Page? _page;
    private int _selectedTileId = -1;
    private Bitmap? _tileset;

    private bool _showGrid = true;

    public MainForm()
    {
        InitializeComponent();
        this.KeyPreview = true;
        this.KeyDown += MainForm_KeyDown;

        _mapView = new MapViewControl
        {
            Location = ViewerConstants.MapViewLocation,
            Size = ViewerConstants.MapViewSize
        };
        Controls.Add(_mapView);

        // テスト用のダミーデータをロード
        LoadTest();
    }

    private void MainForm_Load(object sender, EventArgs e)
    {
    }
}
