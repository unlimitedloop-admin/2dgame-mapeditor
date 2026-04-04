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

    /// <summary>
    /// JSONファイルからプロジェクトを読み込む
    /// </summary>
    public EditorProject Load(string path)
    {
        string json = File.ReadAllText(path);
        var project = JsonSerializer.Deserialize<EditorProject>(json, _options);

        return project is null ? throw new InvalidOperationException($"Failed to deserialize project from: {path}") : project;
    }

    /// <summary>
    /// プロジェクトをJSONファイルに保存する
    /// </summary>
    public void Save(EditorProject project, string path)
    {
        string json = JsonSerializer.Serialize(project, _options);
        File.WriteAllText(path, json);
    }
}
