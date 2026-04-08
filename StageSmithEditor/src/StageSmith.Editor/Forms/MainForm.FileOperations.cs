using StageSmith.Application.Commands;
using StageSmith.Core.Constants;
using StageSmith.Infrastructure.Persistence;

namespace StageSmith.Editor;

public partial class MainForm
{
    private void btnSave_Click(object? sender, EventArgs e)
    {
        var repository = new JsonProjectRepository();
        repository.Save(_project!, "test_project.def");
    }

    private void LoadTest()
    {
        var repository = new JsonProjectRepository();
        _project = repository.Load("test_project.def");

        _stage = _project.Stages.FirstOrDefault();
        _page = _stage?.Pages.FirstOrDefault();

        if (_page == null || string.IsNullOrEmpty(_stage?.TilesetImagePath))
            return;

        _tileset = new Bitmap(_stage.TilesetImagePath);

        // --- TilePalette ---
        _tilePalette.SetTileset(_tileset);

        _tilePalette.TileSelected += index =>
        {
            _selectedTileId = index;
        };

        // --- MapView ---
        _mapView.SetTileMap(_page.TileMap);
        _mapView.SetTileset(_tileset);

        // --- Tool 初期化 ---
        InitializeTools();

        // --- Tool 設定 ---
        _mapView.CurrentTool = _penTool;
        _mapView.PickerTool = _pickerTool;

        _mapView.Invalidate();
        _tilePalette.Invalidate();
    }
}
