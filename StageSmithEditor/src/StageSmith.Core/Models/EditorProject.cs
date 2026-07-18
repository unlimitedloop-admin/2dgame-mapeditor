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
    public List<Stage> Stages { get; set; } = [];
    public List<Bookmark> Bookmarks { get; set; } = [];

    [JsonIgnore]
    public bool HasStages => Stages.Count > 0;

    /// <summary>
    /// JSON読込後の安全化処理。
    /// 古いプロジェクトファイルや不完全なJSONから復元した場合の不整合を補正する。
    /// </summary>
    public void Normalize()
    {
        Name ??= "New Project";
        BaseDirectory ??= string.Empty;
        Stages ??= [];
        Bookmarks ??= [];

        foreach (var stage in Stages)
        {
            stage.Normalize();
        }

        foreach (var bookmark in Bookmarks)
        {
            // HACK: 将来の拡張用にブックマークの正規化処理を追加する場合はここに記述
        }
    }

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
