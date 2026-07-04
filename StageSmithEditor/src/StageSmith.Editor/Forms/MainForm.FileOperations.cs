using StageSmith.Application.Services;
using StageSmith.Core.Constants;
using StageSmith.Infrastructure;
using StageSmith.Infrastructure.Persistence;

namespace StageSmith.Editor;

public partial class MainForm
{
    private string? _currentProjectPath;

    private void NewProject()
    {
        var project = ProjectFactory.CreateNewProject();

        _currentProjectPath = null;

        // 前のプロジェクトの表示をクリア
        ResetView();

        _context.Project = project;
        _context.SetStage(0);
        _context.SetPage(0);

        BindStageExplorer();
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

        // 前のプロジェクトの表示をクリア
        ResetView();

        _context.Project = project;
        _context.SetStage(0);
        _context.SetPage(0);

        BindStageExplorer();
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

    private void ExportStageDef(string filename)
    {
        var stage = _context.CurrentStage;
        if (stage == null) return;

        var path = filename;
        DefExporter.Export(stage, path);
    }

    private void ExportCurrentStageDefAs()
    {
        var stage = _context.CurrentStage;

        if (stage == null)
        {
            MessageBox.Show(
                "出力対象のステージがありません。",
                "Export DEF",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        using var dialog = new SaveFileDialog
        {
            Title = "DEFファイルを出力",
            Filter = FileExtensions.StageDefinition + "|*" + FileExtensions.StageDefinition + "|All files (*.*)|*.*",
            FileName = $"{stage.Name}{FileExtensions.StageDefinition}"
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        ExportStageDef(dialog.FileName);    

        MessageBox.Show(
            $"DEF出力しました。\n{dialog.FileName}",
            "Export DEF",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
    }

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

        // 4. defファイルも同じ場所に出力 (基本的にbinだけ出力する事はない想定のため)
        ExportStageDef(Path.ChangeExtension(dialog.FileName, FileExtensions.StageDefinition));

        MessageBox.Show(
            $"BIN出力しました。\n{dialog.FileName}\n{bytes.Length} bytes",
            "Export BIN",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
    }

    private void OpenTilesetImage()
    {
        var stage = _context.CurrentStage;

        if (stage == null)
        {
            MessageBox.Show("タイル画像を設定するステージがありません。");
            return;
        }

        using var dialog = new OpenFileDialog
        {
            Title = "タイルセット画像を選択",
            Filter = FileExtensions.ImageFilter
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        stage.TilesetImagePath = dialog.FileName;

        LoadTilesetImage(dialog.FileName);

        _tilePalette.Invalidate();
        _mapView.Invalidate();
    }

    private void LoadTilesetImage(string path)
    {
        if (!File.Exists(path))
        {
            MessageBox.Show(
                $"タイル画像が見つかりません。\n{path}",
                "Tileset",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        try
        {
            using var loaded = new Bitmap(path);

            if (loaded.Width % MapConstants.DefaultTileSize != 0 ||
                loaded.Height % MapConstants.DefaultTileSize != 0)
            {
                MessageBox.Show(
                    $"タイルセット画像のサイズが不正です。\n" +
                    $"{MapConstants.DefaultTileSize}px単位で割り切れる画像を指定してください。\n\n" +
                    $"画像サイズ: {loaded.Width} x {loaded.Height}",
                    "Tileset",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            var bitmap = new Bitmap(loaded);

            _tileset?.Dispose();
            _tileset = bitmap;

            _tilePalette.SetTileset(_tileset);
            _mapView.SetTileset(_tileset);
            _metaTilePalette.SetTileset(_tileset);

            // 選択状態をリセット
            _selectedTileId = -1;
            _tilePalette.SetSelected(-1);
            _mapView.PreviewTileId = -1;

            // ノードエディタが開いていればプレビューキャッシュを再生成する
            _nodeEditorForm?.SyncTileset(_tileset);
        }
        catch (Exception ex) when (
            ex is ArgumentException ||
            ex is IOException ||
            ex is UnauthorizedAccessException)
        {
            MessageBox.Show(
                $"タイル画像の読み込みに失敗しました。\n{path}\n\n{ex.Message}",
                "Tileset",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    /// <summary>
    /// 現在のステージに新規ページを追加し、追加したページへ移動する。
    /// </summary>
    private void AddPageToCurrentStage()
    {
        var stage = _context.CurrentStage;
        if (stage == null)
        {
            MessageBox.Show(
                "ページを追加するステージがありません。",
                "ページ追加",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        var page = stage.AddPage();

        // 追加したページへ移動
        var newIndex = stage.Pages.IndexOf(page);
        _context.SetPage(newIndex);

        // ツリーと表示を更新
        _stageExplorer.RebuildTree();
        ApplyContextToView();
    }

    /// <summary>
    /// プロジェクト切り替え前にマップビュー・タイルパレット・プロパティウィンドウの表示をすべてクリアする。
    /// </summary>
    private void ResetView()
    {
        _page = null;
        _tileset?.Dispose();
        _tileset = null;
        _selectedTileId = -1;

        _mapView.SetTileMap(null);
        _mapView.SetTileset(null);
        _mapView.PreviewTileId = -1;

        _tilePalette.SetTileset(null);
        _metaTilePalette.SetStage(null);
        _metaTilePalette.SetTileset(null);
        _context.SetSelectedMetaTile(null);

        _propertyWindow.RefreshProperties();
        _pageNavBar.UpdateDisplay(_context);

        _mapView.Invalidate();
        _tilePalette.Invalidate();
        _metaTilePalette.RefreshPalette();
    }
}
