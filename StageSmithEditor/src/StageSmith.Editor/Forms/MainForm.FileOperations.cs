using StageSmith.Application.Commands;
using StageSmith.Application.Services;
using StageSmith.Core.Constants;
using StageSmith.Core.Models;
using StageSmith.Editor.Forms;
using StageSmith.Infrastructure;
using StageSmith.Infrastructure.Persistence;

namespace StageSmith.Editor;

public partial class MainForm
{
    private string? _currentProjectPath;
    private int _savedUndoCount = 0;

    private void NewProject()
    {
        if (!ConfirmDiscardChangesIfNeeded()) return;

        var project = ProjectFactory.CreateNewProject();

        // このプロジェクトのステージ保存構造を、作成時点のエディタ設定で固定する
        project.UseStageSubFolder = _config.UseStageSubFolder ? true : null;

        _currentProjectPath = null;

        ResetView();

        _context.Project = project;
        _context.SetStage(0);
        _context.SetPage(0);

        _commandManager.Clear();
        _savedUndoCount = 0;

        BindStageExplorer();
        BindBookmarkList();
        ApplyContextToView();

        UpdateEditorAvailability();
        UpdateTitle();
    }

    private void OpenProject()
    {
        if (!ConfirmDiscardChangesIfNeeded()) return;

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

        ResetView();

        _context.Project = project;

        var stageIndex = project.ResolveLastEditedStageIndex() ?? 0;
        _context.SetStage(stageIndex); // 内部でPageIndexは一旦0になる

        var pageIndex = project.LastEditedPageIndex;
        if (pageIndex > 0 && pageIndex < (_context.CurrentStage?.Pages.Count ?? 0))
        {
            _context.SetPage(pageIndex);
        }

        _commandManager.Clear();
        _savedUndoCount = 0;

        BindStageExplorer();
        BindBookmarkList();
        ApplyContextToView();

        UpdateEditorAvailability();
        UpdateTitle();

        ShowStatusMessage($"{TruncatePathForStatus(_currentProjectPath)} からプロジェクトファイル(.sseproj)を読み込みました");
    }

    private void CloseProject()
    {
        if (_context.Project == null) return;

        if (!ConfirmDiscardChangesIfNeeded()) return;

        _currentProjectPath = null;
        _context.Project = null;

        ResetView();
        BindStageExplorer();
        BindBookmarkList();

        _commandManager.Clear();
        _savedUndoCount = 0;

        UpdateEditorAvailability();
        UpdateTitle();
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

        _context.Project.LastEditedStageId = _context.CurrentStage?.Id;
        _context.Project.LastEditedPageIndex = _context.CurrentPageIndex;

        repository.Save(_context.Project, _currentProjectPath);

        _savedUndoCount = _commandManager.UndoCount;

        _stageExplorer.RebuildTree();

        ShowStatusMessage($"{TruncatePathForStatus(_currentProjectPath)} へプロジェクトを新規保存しました");
        UpdateTitle();
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

        if (!string.IsNullOrWhiteSpace(_config.DefaultProjectSaveDirectory) &&
            Directory.Exists(_config.DefaultProjectSaveDirectory))
        {
            dialog.InitialDirectory = _config.DefaultProjectSaveDirectory;
        }

        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        _currentProjectPath = ResolveNewProjectPath(dialog.FileName);

        var repository = new JsonProjectRepository();

        _context.Project.LastEditedStageId = _context.CurrentStage?.Id;
        _context.Project.LastEditedPageIndex = _context.CurrentPageIndex;

        repository.Save(_context.Project, _currentProjectPath);

        _savedUndoCount = _commandManager.UndoCount;

        _stageExplorer.RebuildTree();
        UpdateTitle();
    }

    /// <summary>
    /// UseProjectSubDirectory が有効な場合、ダイアログで選んだパスの下に
    /// プロジェクト名フォルダを1つ噛ませたパスへ差し替える。
    /// 既存プロジェクトの上書き保存（SaveProject）はこの経路を通らないため影響しない。
    /// </summary>
    private string ResolveNewProjectPath(string dialogSelectedPath)
    {
        if (!_config.UseProjectSubDirectory)
            return dialogSelectedPath;

        var projectName = Path.GetFileNameWithoutExtension(dialogSelectedPath);
        var parentDir = Path.GetDirectoryName(dialogSelectedPath) ?? string.Empty;
        var subDir = Path.Combine(parentDir, projectName);

        Directory.CreateDirectory(subDir);

        return Path.Combine(subDir, Path.GetFileName(dialogSelectedPath));
    }

    /// <summary>
    /// 現在のプロジェクトに未保存の変更があるかどうかを判定する。
    /// 保存時点のUndoStackの深さと現在の深さを比較する。
    /// Undoで保存時点まで巻き戻した場合は自動的に「変更なし」に戻る。
    /// </summary>
    private bool IsProjectDirty()
    {
        return _context.Project != null && _commandManager.UndoCount != _savedUndoCount;
    }

    /// <summary>
    /// 未保存の変更がある場合、保存するかどうかを確認する。
    /// New/Open/Close/アプリ終了など、プロジェクトを手放す操作の直前に呼び出す。
    /// </summary>
    /// <returns>操作を続行してよい場合は true、中断すべき場合は false。</returns>
    private bool ConfirmDiscardChangesIfNeeded()
    {
        if (!IsProjectDirty()) return true;

        var result = MessageBox.Show(
            this,
            "編集内容が保存されていません。保存しますか？",
            "確認",
            MessageBoxButtons.YesNoCancel,
            MessageBoxIcon.Warning);

        switch (result)
        {
            case DialogResult.Yes:
                SaveProject();
                return !IsProjectDirty(); // SaveAsをキャンセルした場合は中断
            case DialogResult.No:
                return true;
            default:
                return false; // Cancel
        }
    }

    /// <summary>
    /// メインフォームのタイトルバーを、プロジェクトの状態に合わせて更新する。
    /// 例: "StageSmith Editor - Project Name.sseproj *"（未保存の変更あり）
    /// </summary>
    private void UpdateTitle()
    {
        if (_context.Project == null)
        {
            Text = "StageSmith Editor";
            return;
        }

        // 保存済みならファイル名（拡張子つき）、未保存の新規プロジェクトならProject.Nameを表示
        var displayName = !string.IsNullOrWhiteSpace(_currentProjectPath)
            ? Path.GetFileName(_currentProjectPath)
            : _context.Project.Name;

        var dirtyMark = IsProjectDirty() ? " *" : "";
        var readOnlyMark = _commandManager.IsReadOnly ? " [Read-Only]" : "";

        Text = $"StageSmith Editor - {displayName}{dirtyMark}{readOnlyMark}";
    }

    /// <summary>
    /// プロジェクトの有無に応じて、各パネルとメニューの有効/無効を切り替える。
    /// </summary>
    private void UpdateEditorAvailability()
    {
        var hasProject = _context.HasProject;

        _mapView.Enabled         = hasProject;
        _tilePalette.Enabled     = hasProject;
        _propertyWindow.Enabled  = hasProject;
        _metaTilePalette.Enabled = hasProject;
        _stageExplorer.Enabled   = hasProject;

        RefreshFileMenuState();
        RefreshEditMenuState();
        RefreshViewMenuState();
        RefreshNavigationMenuState();
    }

    private void NewStage()
    {
        if (BlockIfReadOnly("New Stage")) return;

        var project = _context.Project;
        if (project == null)
        {
            MessageBox.Show(
                this,
                "プロジェクトが開かれていません。",
                "New Stage",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        var defaultName = GenerateDefaultStageName(project);

        using var dialog = new NewStageDialog(defaultName);
        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        var stage = project.AddStage(dialog.StageName);

        var newIndex = project.Stages.IndexOf(stage);
        _context.SetStage(newIndex);

        _stageExplorer.RebuildTree();
        ApplyContextToView();
    }

    private void SaveStage()
    {
        var project = _context.Project;
        var stage = _context.CurrentStage;

        if (project == null || stage == null)
        {
            MessageBox.Show(
                "保存対象のステージがありません。",
                "Save Stage",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        var repository = new JsonProjectRepository();
        var stageDir = project.ResolveStageStorageDirectory();
        Directory.CreateDirectory(stageDir);

        var path = !string.IsNullOrWhiteSpace(stage.FilePath)
            ? stage.FilePath
            : JsonProjectRepository.ResolveStageFilePath(stage, stageDir, project.UseStageSubFolder == true);

        repository.SaveStage(stage, path);

        _stageExplorer.RebuildTree();
        ShowStatusMessage($"{TruncatePathForStatus(path)} へステージを保存しました");
    }

    /// <summary>
    /// 現在のステージを、最後に保存した状態までリロードする。
    /// </summary>
    private void ReloadStage()
    {
        var project = _context.Project;
        var stage = _context.CurrentStage;

        if (project == null || stage == null)
        {
            MessageBox.Show(
                "リロード対象のステージがありません。",
                "Reload Stage",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(stage.FilePath))
        {
            MessageBox.Show(
                this,
                "このステージはまだ一度も保存されていないため、リロードできません。",
                "Reload Stage",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        var confirm = MessageBox.Show(
            this,
            $"「{stage.Name}」を最後に保存した状態まで戻します。\n" +
            "現在の未保存の変更は失われます。よろしいですか？",
            "Reload Stage",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning);

        if (confirm != DialogResult.Yes) return;

        Stage reloaded;

        try
        {
            var repository = new JsonProjectRepository();
            reloaded = repository.LoadStage(stage.FilePath);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                this,
                $"ステージファイルの読み込みに失敗しました。\n{stage.FilePath}\n{ex.Message}",
                "Reload Stage",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            return;
        }

        var stageIndex = project.Stages.IndexOf(stage);
        if (stageIndex < 0) return;

        var pageIndexBeforeReload = _context.CurrentPageIndex;

        project.Stages[stageIndex] = reloaded;

        // ステージを丸ごと差し替えるため、既存のUndo履歴は整合性を保てなくなる。
        // 他ステージ分も含め、New/Open Projectと同じ扱いで全クリアする（合意済み）。
        _commandManager.Clear();
        _savedUndoCount = 0;

        _context.SetStage(stageIndex); // ContextChanged経由でビュー全体が再同期される

        // リロード後もページ位置を維持できる場合は維持する（SetStageで一旦0に戻るため）
        if (pageIndexBeforeReload > 0 && pageIndexBeforeReload < reloaded.Pages.Count)
        {
            _context.SetPage(pageIndexBeforeReload);
        }

        UpdateTitle();
        ShowStatusMessage($"「{reloaded.Name}」を {TruncatePathForStatus(stage.FilePath)} から再読込しました");
    }

    private void DropStage()
    {
        if (BlockIfReadOnly("Drop Stage")) return;

        var stage = _context.CurrentStage;
        if (stage == null) return;

        var confirm = MessageBox.Show(
            this,
            $"ステージ「{stage.Name}」を削除します。\nこの操作は元に戻せません。よろしいですか？",
            "Drop Stage",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning);

        if (confirm != DialogResult.Yes) return;

        DeleteStage(stage);
    }

    /// <summary>
    /// 指定したステージをプロジェクトから削除する。
    /// NOTE: 破壊的・不可逆操作（.ssestage の物理削除まで行う）。Undo対象外。
    /// 呼び出しは「File > Drop Stage」と「Stage Explorer の Delete」の
    /// 確認ダイアログ通過後のみに限定すること。それ以外から直接呼び出さないこと。
    /// </summary>
    private void DeleteStage(Stage stage)
    {
        var project = _context.Project;
        if (project == null) return;

        var filePath = stage.FilePath;
        var stageName = stage.Name;
        var wasCurrent = ReferenceEquals(_context.CurrentStage, stage);

        project.RemoveStage(stage.Id); // 紐づくブックマークも合わせて削除される（EditorProject.RemoveStage内）

        if (!string.IsNullOrWhiteSpace(filePath))
        {
            var repository = new JsonProjectRepository();
            repository.DeleteStageFile(filePath); // NOTE: 物理削除
        }

        if (wasCurrent)
        {
            _context.SetStage(0);
            ResetView();
            ApplyContextToView();
        }

        _stageExplorer.RebuildTree();
        _bookmarkList.RefreshList();

        ShowStatusMessage($"ステージ「{stageName}」を削除しました");
    }

    /// <summary>
    /// "Stage 001" 形式で、まだ使われていない最小の連番を割り当てた初期名を生成する。
    /// </summary>
    private static string GenerateDefaultStageName(EditorProject project)
    {
        var usedNumbers = new HashSet<int>();
        var regex = new System.Text.RegularExpressions.Regex(@"^Stage (\d{3})$");

        foreach (var stage in project.Stages)
        {
            var match = regex.Match(stage.Name);
            if (match.Success && int.TryParse(match.Groups[1].Value, out var number))
            {
                usedNumbers.Add(number);
            }
        }

        var candidate = 1;
        while (usedNumbers.Contains(candidate))
            candidate++;

        return $"Stage {candidate:D3}";
    }

    private void ExportStageDef(string filename)
    {
        var stage = _context.CurrentStage;
        if (stage == null) return;

        var path = filename;
        DefExporter.Export(stage, path);
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

        ShowStatusMessage($"{TruncatePathForStatus(dialog.FileName)} へBINファイルを出力しました（{bytes.Length} bytes）");

        MessageBox.Show(
            $"BIN出力しました。\n{dialog.FileName}\n{bytes.Length} bytes",
            "Export BIN",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
    }

    private void ExportAllStages()
    {
        var project = _context.Project;

        if (project == null || !project.HasStages)
        {
            MessageBox.Show(
                this,
                "出力対象のステージがありません。",
                "Export All Stages",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        using var folderDialog = new FolderBrowserDialog
        {
            Description = "出力先フォルダを選択",
        };

        if (folderDialog.ShowDialog(this) != DialogResult.OK) return;

        var outputDir = folderDialog.SelectedPath;

        // TODO: 将来的にエディタ設定で「ステージごとにサブフォルダを分ける」オプションを追加する場合は
        //       ここで outputDir をステージ単位のサブフォルダに切り替える分岐を入れる。
        var exportedCount = 0;

        foreach (var stage in project.Stages)
        {
            var baseName = SanitizeFileName(stage.Name);

            var binPath = Path.Combine(outputDir, $"{baseName}{FileExtensions.StageMapBinary}");
            var defPath = Path.Combine(outputDir, $"{baseName}{FileExtensions.StageDefinition}");

            var bytes = stage.ExportBin();
            File.WriteAllBytes(binPath, bytes);

            DefExporter.Export(stage, defPath);

            exportedCount++;
        }

        MessageBox.Show(
            this,
            $"{exportedCount} 件のステージを出力しました。\n{outputDir}",
            "Export All Stages",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);

        ShowStatusMessage($"{exportedCount} 件のステージを {TruncatePathForStatus(outputDir)} へ出力しました");
    }

    /// <summary>
    /// ファイル名として使用できない文字を "_" に置換する。
    /// ステージ名は自由入力のため、パス区切り文字などが含まれる可能性がある。
    /// </summary>
    private static string SanitizeFileName(string name)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = new string(name.Select(c => invalidChars.Contains(c) ? '_' : c).ToArray());
        return string.IsNullOrWhiteSpace(sanitized) ? "Stage" : sanitized;
    }

    private void OpenTilesetImage()
    {
        if (BlockIfReadOnly("Import Tileset")) return;

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
            _findTileDialog?.SetTileset(_tileset);
            _replaceTileDialog?.SetTileset(_tileset);

            // 選択状態をリセット
            _selectedTileId = -1;
            _tilePalette.SetSelected(-1);
            _mapView.PreviewTileId = -1;

            // ノードエディタが開いていればプレビューキャッシュを再生成する
            _nodeEditorForm?.SyncTileset(_tileset);

            ShowStatusMessage($"{TruncatePathForStatus(path)} からタイルセット画像を読み込みました");
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
    /// タイルセット未設定のステージへ切り替わった際、前のステージの表示を持ち越さないための後始末。
    /// LoadTilesetImage() の成功パスと対になる「空にする」版。
    /// </summary>
    private void ClearTileset()
    {
        _tileset?.Dispose();
        _tileset = null;

        _tilePalette.SetTileset(null);
        _mapView.SetTileset(null);
        _metaTilePalette.SetTileset(null);
        _findTileDialog?.SetTileset(null);
        _replaceTileDialog?.SetTileset(null);

        _selectedTileId = -1;
        _tilePalette.SetSelected(-1);
        _mapView.PreviewTileId = -1;

        _nodeEditorForm?.SyncTileset(null);
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

        _commandManager.Execute(new AddPageCommand(stage, _context));
    }

    private void RemoveCurrentPage()
    {
        var stage = _context.CurrentStage;
        var page = _context.CurrentPage;
        
        if (stage == null || page == null)
        {
            MessageBox.Show(
                "削除対象のページがありません。",
                "ページ削除",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        var confirm = MessageBox.Show(
            this,
            $"ページ「{page.Name}」を削除しますか？",
            "ページの削除",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning
        );
        
        if (confirm != DialogResult.Yes) return;
        
        _commandManager.Execute(new RemovePageCommand(stage, page, _context));
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
        _loadedTilesetStageId = null;

        _mapView.SetTileMap(null);
        _mapView.SetTileset(null);
        _mapView.PreviewTileId = -1;

        _tilePalette.SetTileset(null);
        _metaTilePalette.SetStage(null);
        _metaTilePalette.SetTileset(null);
        _findTileDialog?.SetTileset(null);
        _replaceTileDialog?.SetTileset(null);
        _context.SetSelectedMetaTile(null);

        _propertyWindow.RefreshProperties();
        _pageNavBar.UpdateDisplay(_context);

        _mapView.Invalidate();
        _tilePalette.Invalidate();
        _metaTilePalette.RefreshPalette();
    }
}
