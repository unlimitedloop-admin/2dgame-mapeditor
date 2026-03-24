using System.Text.Json.Serialization;

namespace StageSmith.Core.Models;

public sealed class EditorProject
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public string Name { get; set; } = "New Project";

    /// <summary>
    /// ステージ定義ファイル保存先ルートなど、将来拡張用
    /// </summary>
    public string BaseDirectory { get; set; } = string.Empty;

    public List<Stage> Stages { get; init; } = new();

    [JsonIgnore]
    public bool HasStages => Stages.Count > 0;

    public Stage AddStage(string name)
    {
        var stage = new Stage
        {
            Name = name
        };

        Stages.Add(stage);
        return stage;
    }

    public bool RemoveStage(Guid stageId)
    {
        var target = Stages.FirstOrDefault(x => x.Id == stageId);
        if (target is null)
        {
            return false;
        }

        return Stages.Remove(target);
    }

    public Stage? FindStage(Guid stageId)
    {
        return Stages.FirstOrDefault(x => x.Id == stageId);
    }
}
