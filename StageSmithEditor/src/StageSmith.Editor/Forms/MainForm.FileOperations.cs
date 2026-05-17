using StageSmith.Infrastructure.Persistence;

namespace StageSmith.Editor;

public partial class MainForm
{
    private void SaveProject()
    {
        var repository = new JsonProjectRepository();
        repository.Save(_project!, "test_project.def");
    }

    private void LoadTest()
    {
        var repository = new JsonProjectRepository();
        _project = repository.Load("test_project.def");

        _context.Project = _project;
        _context.SetStage(0);
        _context.SetPage(0);

        _stage = _context.CurrentStage;
        _page = _context.CurrentPage;

        if (_page == null || string.IsNullOrEmpty(_stage?.TilesetImagePath))
            return;

        _tileset = new Bitmap(_stage.TilesetImagePath);

        // --- TilePalette ---
        _tilePalette.SetTileset(_tileset);

        _tilePalette.TileSelected += index =>
        {
            _selectedTileId = index;
            _mapView.PreviewTileId = index;
        };

        // --- MapView ---
        _mapView.SetTileMap(_page.TileMap);
        _mapView.SetTileset(_tileset);

        // --- Tool 初期化 ---
        InitializeTools();

        // --- Tool 設定 ---
        _mapView.ToolManager?.SetTool(_penTool!);
        _mapView.PickerTool = _pickerTool;

        _mapView.Invalidate();
        _tilePalette.Invalidate();
    }

    private void ExportCurrentStageBinTest()
    {
        var stage = _context.CurrentStage;

        if (stage == null)
        {
            MessageBox.Show("出力対象のステージがありません。");
            return;
        }

        using var dialog = new SaveFileDialog
        {
            Title = "BINファイルを出力",
            Filter = "BIN files (*.bin)|*.bin|All files (*.*)|*.*",
            FileName = $"{stage.Name}.bin"
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        var bytes = stage.ExportBin();

        File.WriteAllBytes(dialog.FileName, bytes);

        MessageBox.Show(
            $"BIN出力しました。\n{dialog.FileName}\n{bytes.Length} bytes",
            "Export BIN",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
    }
}
