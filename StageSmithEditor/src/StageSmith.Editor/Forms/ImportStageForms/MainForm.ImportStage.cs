using StageSmith.Core.Constants;
using StageSmith.Core.Models;
using StageSmith.Infrastructure.Persistence;

namespace StageSmith.Editor;

public partial class MainForm
{
    /// <summary>
    /// .ssestage ファイルを選択してステージをインポートする。
    /// 選択したファイルはプロジェクトの BaseDirectory 配下へコピーしてから登録する。
    /// </summary>
    private void ImportStage()
    {
        if (BlockIfReadOnly("Import Stage")) return;

        var project = _context.Project;

        if (project == null)
        {
            MessageBox.Show(
                this,
                "プロジェクトが開かれていません。",
                "Import Stage",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        using var openDialog = new OpenFileDialog
        {
            Filter = FileExtensions.StageFileFilter,
            Title  = "インポートするステージファイルを選択",
            Multiselect = true,
        };

        if (openDialog.ShowDialog(this) != DialogResult.OK) return;

        var repository = new JsonProjectRepository();
        var stageDir = project.ResolveStageStorageDirectory();
        Directory.CreateDirectory(stageDir);

        var importedNames = new List<string>();

        foreach (var sourceFilePath in openDialog.FileNames)
        {
            Stage sourceStage;

            try
            {
                sourceStage = repository.LoadStage(sourceFilePath);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    this,
                    $"ステージファイルの読み込みに失敗しました。\n{sourceFilePath}\n{ex.Message}",
                    "Import Stage",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                continue;
            }

            // Clone() で FilePath=null / IsDirty=true にリセットされる
            // （インポート元ファイルを誤って上書きしないため）
            var importedStage = sourceStage.Clone();

            if (!ResolveStageConflicts(importedStage))
                continue; // このステージだけスキップして次へ

            // BaseDirectory 配下へ物理コピーする
            var destPath = JsonProjectRepository.ResolveStageFilePath(importedStage, stageDir);
            repository.SaveStage(importedStage, destPath);

            project.Stages.Add(importedStage);
            importedNames.Add(importedStage.Name);
        }

        if (importedNames.Count == 0) return;

        // ステージファイル自体は保存済みだが、ステージ一覧（.sseproj）は未保存なので記録する
        MarkUnrecordedChange();

        _stageExplorer.RebuildTree();

        MessageBox.Show(
            this,
            $"{importedNames.Count} 件のステージをインポートしました。\n{string.Join("\n", importedNames)}",
            "Import Stage",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
    }

    /// <summary>
    /// 既存ステージとの重複を解決する。
    /// Key / StageNumber はエディタ内部の管理値でユーザーが直接編集できないため、
    /// 衝突していても確認なしでそのまま自動採番する。
    /// Name はユーザーが目にする識別子なので、衝突時のみ確認する。
    /// </summary>
    private bool ResolveStageConflicts(Stage importedStage)
    {
        var project = _context.Project!;

        if (!string.IsNullOrEmpty(importedStage.Key)
            && project.Stages.Any(s => s.Key == importedStage.Key))
        {
            importedStage.Key = GenerateUniqueKey(project, importedStage.Key);
        }

        if (project.Stages.Any(s => s.StageNumber == importedStage.StageNumber))
        {
            importedStage.StageNumber = GenerateUniqueStageNumber(project);
        }

        var nameConflict = project.Stages.Any(s => s.Name == importedStage.Name);
        if (!nameConflict) return true;

        var newName = GenerateUniqueName(project, importedStage.Name);

        var message =
            $"ステージ名「{importedStage.Name}」は既存のステージと重複しています。\n" +
            $"「{newName}」として取り込みますか？";

        var result = MessageBox.Show(
            this,
            message,
            "Import Stage - 名前の重複",
            MessageBoxButtons.OKCancel,
            MessageBoxIcon.Warning);

        if (result != DialogResult.OK) return false;

        importedStage.Name = newName;
        return true;
    }

    private static string GenerateUniqueKey(EditorProject project, string baseKey)
    {
        var existingKeys = project.Stages.Select(s => s.Key).ToHashSet();

        var suffix    = 2;
        var candidate = $"{baseKey}_import";

        while (existingKeys.Contains(candidate))
        {
            candidate = $"{baseKey}_import{suffix}";
            suffix++;
        }

        return candidate;
    }

    private static int GenerateUniqueStageNumber(EditorProject project)
    {
        return project.Stages.Count == 0
            ? 0
            : project.Stages.Max(s => s.StageNumber) + 1;
    }

    private static string GenerateUniqueName(EditorProject project, string baseName)
    {
        var existingNames = project.Stages.Select(s => s.Name).ToHashSet();

        var suffix    = 2;
        var candidate = $"{baseName} (Import)";

        while (existingNames.Contains(candidate))
        {
            candidate = $"{baseName} (Import {suffix})";
            suffix++;
        }

        return candidate;
    }
}
