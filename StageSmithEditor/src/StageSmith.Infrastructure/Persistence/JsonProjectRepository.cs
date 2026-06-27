using System.Text.Json;
using StageSmith.Application.Services;
using StageSmith.Core.Models;

namespace StageSmith.Infrastructure.Persistence;

/// <summary>
/// JSONファイルを使用したプロジェクトの永続化実装
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

    // プロジェクトの保存と読み込みをJSON形式で行う
    public void Save(EditorProject project, string path)
    {
        ArgumentNullException.ThrowIfNull(project);

        // 保存前にも正規化しておく。
        // これにより、古いデータを読み込んでそのまま保存した場合も安定する。
        project.Normalize();

        var json = JsonSerializer.Serialize(project, _options);

        File.WriteAllText(path, json);
    }

    // 指定されたパスからプロジェクトを読み込む
    public EditorProject Load(string path)
    {
        var json = File.ReadAllText(path);

        var project = JsonSerializer.Deserialize<EditorProject>(json, _options)
            ?? throw new InvalidOperationException("プロジェクトファイルの読み込みに失敗しました。");

        project.Normalize();

        return project;
    }
}
