using System.Text.Json;
using StageSmith.Application.Services;
using StageSmith.Core.Constants;
using StageSmith.Core.Models;

namespace StageSmith.Infrastructure.Persistence;

/// <summary>
/// JSON形式でプロジェクト・ステージを永続化するリポジトリ。
/// </summary>
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

        var useSubFolder = project.UseStageSubFolder == true;

        foreach (var stage in project.Stages)
        {
            var stageFilePath = ResolveStageFilePath(stage, stageDir, useSubFolder);

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
                // 仕様: ファイルが見つからないステージ参照は意図的に読み飛ばす。
                // 次回 Save Project 時、StageFilePaths は現在の Stages から再構築されるため、
                // 見つからなかった参照は自動的に .sseproj から除去される。
                // ファイル整理などでステージファイルを移動・削除した場合の責任はユーザー側にあるものとし、
                // エディタ側からの復旧導線（再指定ダイアログ等）はあえて設けない。
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

    public static string ResolveStageFilePath(Stage stage, string stageDir, bool useSubFolder = false)
    {
        if (!string.IsNullOrWhiteSpace(stage.FilePath))
            return stage.FilePath;

        var extension = FileExtensions.StageFile;

        var baseName = !string.IsNullOrWhiteSpace(stage.Name) ? stage.Name : stage.Key;
        if (string.IsNullOrWhiteSpace(baseName))
            baseName = "Stage";

        var safeName = SanitizeFileName(baseName);

        var targetDir = stageDir;
        if (useSubFolder)
        {
            targetDir = Path.Combine(stageDir, safeName);
            Directory.CreateDirectory(targetDir);
        }

        var candidate = Path.Combine(targetDir, $"{safeName}{extension}");
 
        var suffix = 2;
        while (File.Exists(candidate))
        {
            candidate = Path.Combine(targetDir, $"{safeName}_{suffix}{extension}");
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
