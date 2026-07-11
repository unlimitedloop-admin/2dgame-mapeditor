using StageSmith.Core.Constants;
using StageSmith.Core.Models;
using StageSmith.Editor.Forms;
using StageSmith.Infrastructure.Persistence;

namespace StageSmith.Editor;

public partial class MainForm
{
    /// <summary>
    /// 他プロジェクト（.sseproj）からステージを1件選んでインポートする。
    /// </summary>
    private void ImportStage()
    {
        if (_context.Project == null)
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
            Filter = FileExtensions.ProjectFilter,
            Title  = "インポート元プロジェクトを選択",
        };

        if (openDialog.ShowDialog(this) != DialogResult.OK) return;

        EditorProject sourceProject;

        try
        {
            var repository = new JsonProjectRepository();
            sourceProject = repository.Load(openDialog.FileName);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                this,
                $"プロジェクトの読み込みに失敗しました。\n{ex.Message}",
                "Import Stage",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            return;
        }

        if (!sourceProject.HasStages)
        {
            MessageBox.Show(
                this,
                "選択したプロジェクトにはステージがありません。",
                "Import Stage",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        using var selectDialog = new ImportStageDialog(sourceProject.Stages);
        if (selectDialog.ShowDialog(this) != DialogResult.OK) return;

        var sourceStage = selectDialog.SelectedStage;
        if (sourceStage == null) return;

        var importedStage = sourceStage.Clone();

        if (!ResolveStageConflicts(importedStage))
            return; // ユーザーがキャンセルした場合はインポート自体を中止

        _context.Project.Stages.Add(importedStage);

        _stageExplorer.RebuildTree();

        MessageBox.Show(
            this,
            $"ステージ「{importedStage.Name}」をインポートしました。",
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
