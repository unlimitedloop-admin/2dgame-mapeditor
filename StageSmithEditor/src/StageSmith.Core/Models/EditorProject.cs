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
    [JsonIgnore]
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

    /// <summary>
    /// このプロジェクトがステージファイルをサブフォルダ分けして保存するかどうか。
    /// プロジェクト新規作成時（NewProject）に、その時点の EditorConfig.UseStageSubFolder を
    /// 一度だけ焼き込む。以降はプロジェクトが存在する限り固定値とし、
    /// エディタ設定(ini)を後から変更しても遡って影響しない。
    /// null（＝OFF）の場合は .sseproj に属性自体を書き出さない。
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? UseStageSubFolder { get; set; }

    /// <summary>
    /// 最後に編集していたステージ／ページ位置（プロジェクトを開いた際の復元用セッション情報）。
    /// ステージの実データではなく、UIの復元用途に限定する。
    /// StageIndexではなくStageIdで持つ理由：将来ステージの並び替え・削除が入ってもズレない安定参照にするため。
    /// </summary>
    public Guid? LastEditedStageId { get; set; }
    public int LastEditedPageIndex { get; set; } = 0;

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
        StageFilePaths ??= [];
        Bookmarks ??= [];
        if (LastEditedPageIndex < 0) LastEditedPageIndex = 0;

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
        if (target is null)
        {
            return false;
        }

        // ステージ削除時、そのステージを参照するブックマークも道連れで削除する。
        // 孤立ブックマーク（存在しないStageIdを指す）を残さないための不変条件。
        Bookmarks.RemoveAll(b => b.StageId == stageId);

        return Stages.Remove(target);
    }

    public Stage? FindStage(Guid stageId)
    {
        return Stages.FirstOrDefault(x => x.Id == stageId);
    }

    /// <summary>
    /// LastEditedStageId に対応する Stages 上のインデックスを解決する。
    /// 該当ステージが見つからない場合（削除済み等）は null を返す。
    /// </summary>
    public int? ResolveLastEditedStageIndex()
    {
        if (LastEditedStageId is null) return null;

        var index = Stages.FindIndex(s => s.Id == LastEditedStageId.Value);
        return index >= 0 ? index : null;
    }
}
