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

    /// <summary>
    /// ステージファイル(.ssestage)の保存先ディレクトリ。
    /// 空の場合は BaseDirectory を使用する（デフォルト挙動）。
    /// 将来の Editor Properties 機能でユーザーが上書き設定できるようにするための窓口。
    /// </summary>
    public string StageStorageDirectory { get; set; } = string.Empty;

    /// <summary>
    /// ステージファイルの実際の保存先ディレクトリを解決する。
    /// StageStorageDirectory が未設定なら BaseDirectory にフォールバックする。
    /// </summary>
    public string ResolveStageStorageDirectory()
        => string.IsNullOrWhiteSpace(StageStorageDirectory) ? BaseDirectory : StageStorageDirectory;

    /// <summary>
    /// 実行時に読み込まれた Stage 一覧。
    /// 正規データは各 .ssestage ファイルなので、.sseproj には直接シリアライズしない。
    /// </summary>
    public List<Stage> Stages { get; set; } = [];

    /// <summary>
    /// .sseproj に保存するステージファイルパス一覧（BaseDirectoryからの相対パス）。
    /// 保存直前に Stages（各 Stage.FilePath）から再構築される。
    /// ロード時はここを読んで各 .ssestage を読み込み、Stages を構築する。
    /// </summary>
    public List<string> StageFilePaths { get; set; } = [];

    /// <summary>
    /// 将来のエディタで使うブックマーク一覧。
    /// </summary>
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
        StageStorageDirectory ??= string.Empty;
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
            Name = name,
            IsDirty = true
        };

        Stages.Add(stage);
        return stage;
    }

    public bool RemoveStage(Guid stageId)
    {
        var target = Stages.FirstOrDefault(x => x.Id == stageId);
        if (target is null) return false;

        return Stages.Remove(target);
    }

    public Stage? FindStage(Guid stageId)
    {
        return Stages.FirstOrDefault(x => x.Id == stageId);
    }
}
