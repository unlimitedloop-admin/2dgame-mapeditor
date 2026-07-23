using System.Text.Json;
using StageSmith.Application.Services;
using StageSmith.Core.Models;

namespace StageSmith.Infrastructure.Persistence;

public sealed class JsonProjectRepository : IProjectRepository
{
    private readonly JsonSerializerOptions _options;

    public JsonProjectRepository()
    {
        _options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = true
        };
    }

    // ===================================================
    // Project (.sseproj)
    // ===================================================

    public void Save(EditorProject project, string path)
    {
        ArgumentNullException.ThrowIfNull(project);

        project.Normalize();

        // BaseDirectory 未設定時は保存先フォルダを採用する
        if (string.IsNullOrWhiteSpace(project.BaseDirectory))
        {
            project.BaseDirectory = Path.GetDirectoryName(Path.GetFullPath(path)) ?? string.Empty;
        }

        var stageDir = project.ResolveStageStorageDirectory();
        Directory.CreateDirectory(stageDir);

        // Stages（実行時オブジェクト）から StageFilePaths（保存用参照）を再構築する
        project.StageFilePaths.Clear();

        foreach (var stage in project.Stages)
        {
            var stageFilePath = ResolveStageFilePath(stage, stageDir);

            // 新規 or 変更済みステージのみ書き込む（未変更ステージへの無駄な書き込みを避ける）
            if (stage.IsDirty || stage.FilePath == null)
            {
                SaveStage(stage, stageFilePath);
            }

            project.StageFilePaths.Add(Path.GetRelativePath(stageDir, stageFilePath));
        }

        var json = JsonSerializer.Serialize(project, _options);
        File.WriteAllText(path, json);
    }

    public EditorProject Load(string path)
    {
        var json = File.ReadAllText(path);

        var project = JsonSerializer.Deserialize<EditorProject>(json, _options)
            ?? throw new InvalidOperationException("プロジェクトファイルの読み込みに失敗しました。");

        project.Normalize();

        if (string.IsNullOrWhiteSpace(project.BaseDirectory))
        {
            project.BaseDirectory = Path.GetDirectoryName(Path.GetFullPath(path)) ?? string.Empty;
        }

        var stageDir = project.ResolveStageStorageDirectory();

        project.Stages.Clear();

        foreach (var relativePath in project.StageFilePaths)
        {
            var stageFilePath = Path.IsPathRooted(relativePath)
                ? relativePath
                : Path.Combine(stageDir, relativePath);

            if (!File.Exists(stageFilePath))
            {
                // BD-004「ファイル未存在→再指定要求」に対応する余地を残し、
                // ここではプロジェクト全体のロードを止めず読み飛ばす。
                // TODO: UI側で「見つからないステージがあります」警告を出す
                continue;
            }

            var stage = LoadStage(stageFilePath);
            project.Stages.Add(stage);
        }

        return project;
    }

    // ===================================================
    // Stage (.ssestage)
    // ===================================================

    public Stage LoadStage(string path)
    {
        var json = File.ReadAllText(path);

        var stage = JsonSerializer.Deserialize<Stage>(json, _options)
            ?? throw new InvalidOperationException("ステージファイルの読み込みに失敗しました。");

        stage.Normalize();
        stage.FilePath = path;
        stage.ClearDirty();

        return stage;
    }

    public void SaveStage(Stage stage, string path)
    {
        ArgumentNullException.ThrowIfNull(stage);

        stage.Normalize();

        var json = JsonSerializer.Serialize(stage, _options);
        File.WriteAllText(path, json);

        stage.FilePath = path;
        stage.ClearDirty();
    }

    /// <summary>
    /// NOTE: 破壊的・不可逆操作。呼び出し元の制約はインターフェース側のコメント参照。
    /// </summary>
    public void DeleteStageFile(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return;
        if (!File.Exists(path)) return;

        File.Delete(path);
    }

    // ===================================================
    // Utilities
    // ===================================================

    private static string ResolveStageFilePath(Stage stage, string stageDir)
    {
        if (!string.IsNullOrWhiteSpace(stage.FilePath))
            return stage.FilePath;

        var baseName = !string.IsNullOrWhiteSpace(stage.Name) ? stage.Name : stage.Key;
        if (string.IsNullOrWhiteSpace(baseName))
            baseName = "Stage";

        var safeName = SanitizeFileName(baseName);
        var candidate = Path.Combine(stageDir, $"{safeName}.ssestage");

        var suffix = 2;
        while (File.Exists(candidate))
        {
            candidate = Path.Combine(stageDir, $"{safeName}_{suffix}.ssestage");
            suffix++;
        }

        return candidate;
    }

    private static string SanitizeFileName(string name)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var chars = name.Select(c => invalidChars.Contains(c) ? '_' : c).ToArray();
        return new string(chars);
    }
}
