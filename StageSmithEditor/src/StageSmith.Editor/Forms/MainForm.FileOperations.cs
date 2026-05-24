using StageSmith.Application.Services;
using StageSmith.Core.Constants;
using StageSmith.Infrastructure.Persistence;

namespace StageSmith.Editor;

public partial class MainForm
{
    private string? _currentProjectPath;

    private void NewProject()
    {
        var project = ProjectFactory.CreateNewProject();

        _currentProjectPath = null;

        _context.Project = project;
        _context.SetStage(0);
        _context.SetPage(0);

        ApplyContextToView();
    }

    private void OpenProject()
    {
        using var dialog = new OpenFileDialog
        {
            Title = "プロジェクトを開く",
            Filter = FileExtensions.ProjectFilter
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        var repository = new JsonProjectRepository();
        var project = repository.Load(dialog.FileName);

        _currentProjectPath = dialog.FileName;

        _context.Project = project;
        _context.SetStage(0);
        _context.SetPage(0);

        ApplyContextToView();
    }

    private void SaveProject()
    {
        if (_context.Project == null)
        {
            MessageBox.Show("保存対象のプロジェクトがありません。");
            return;
        }

        if (string.IsNullOrWhiteSpace(_currentProjectPath))
        {
            SaveProjectAs();
            return;
        }

        var repository = new JsonProjectRepository();
        repository.Save(_context.Project, _currentProjectPath);
    }

    private void SaveProjectAs()
    {
        if (_context.Project == null)
        {
            MessageBox.Show("保存対象のプロジェクトがありません。");
            return;
        }

        using var dialog = new SaveFileDialog
        {
            Title = "プロジェクトを保存",
            Filter = FileExtensions.ProjectFilter,
            FileName = $"{_context.Project.Name}{FileExtensions.Project}"
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        _currentProjectPath = dialog.FileName;

        var repository = new JsonProjectRepository();
        repository.Save(_context.Project, _currentProjectPath);
    }

    // TODO: まもなく削除されます
    //private void LoadTest()
    //{
    //    var repository = new JsonProjectRepository();
    //    _project = repository.Load("test_project.def");

    //    _context.Project = _project;
    //    _context.SetStage(0);
    //    _context.SetPage(0);

    //    _stage = _context.CurrentStage;
    //    _page = _context.CurrentPage;

    //    if (_page == null || string.IsNullOrEmpty(_stage?.TilesetImagePath))
    //        return;

    //    _tileset = new Bitmap(_stage.TilesetImagePath);

    //    // --- TilePalette ---
    //    _tilePalette.SetTileset(_tileset);

    //    _tilePalette.TileSelected += index =>
    //    {
    //        _selectedTileId = index;
    //        _mapView.PreviewTileId = index;
    //    };

    //    // --- MapView ---
    //    _mapView.SetTileMap(_page.TileMap);
    //    _mapView.SetTileset(_tileset);

    //    // --- Tool 初期化 ---
    //    InitializeTools();

    //    // --- Tool 設定 ---
    //    _mapView.ToolManager?.SetTool(_penTool!);
    //    _mapView.PickerTool = _pickerTool;

    //    _mapView.Invalidate();
    //    _tilePalette.Invalidate();
    //}

    private void ExportCurrentStageBin()
    {
        var stage = _context.CurrentStage;

        if (stage == null)
        {
            MessageBox.Show(
                "出力対象のステージがありません。",
                "Export BIN",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        // 1. 確認ダイアログ
        var confirm = MessageBox.Show(
            $"「{stage.Name}」をバイナリファイルへ出力しますか？",
            "Export BIN",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (confirm != DialogResult.Yes)
            return;

        // 2. 保存先ファイル名の指定
        using var dialog = new SaveFileDialog
        {
            Title = "BINファイルを出力",
            Filter = FileExtensions.BinaryFilter,
            FileName = $"{stage.Name}{FileExtensions.StageMapBinary}"
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        // 3. バイナリ出力
        var bytes = stage.ExportBin();

        File.WriteAllBytes(dialog.FileName, bytes);

        MessageBox.Show(
            $"BIN出力しました。\n{dialog.FileName}\n{bytes.Length} bytes",
            "Export BIN",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
    }

    private void ApplyContextToView()
    {
        var page = _context.CurrentPage;

        _page = page;

        _mapView.SetTileMap(page?.TileMap);

        _propertyWindow.RefreshProperties();

        _mapView.Invalidate();
    }
}
