using StageSmith.Core.Models;

namespace StageSmith.Application.Services;

/// <summary>
/// プロジェクトおよびステージの永続化を担当するリポジトリインターフェース
/// </summary>
public interface IProjectRepository
{
    /// <summary>
    /// 指定されたパスからプロジェクトを読み込む。
    /// StageFilePaths を辿って各 .ssestage も合わせてロードする。
    /// </summary>
    EditorProject Load(string path);

    /// <summary>
    /// プロジェクトを指定されたパスに保存する。
    /// ダーティな Stage は合わせて .ssestage として保存される。
    /// </summary>
    void Save(EditorProject project, string path);

    /// <summary>
    /// 指定されたパスから単一のステージ (.ssestage) を読み込む。
    /// </summary>
    Stage LoadStage(string path);

    /// <summary>
    /// ステージを指定されたパスに .ssestage として保存する。
    /// </summary>
    void SaveStage(Stage stage, string path);

    /// <summary>
    /// .ssestage ファイルを物理削除する。
    /// NOTE: 破壊的・不可逆操作（Undo対象外）。
    /// 呼び出しは、ユーザーが明示的な確認ダイアログを経て実行する
    /// 「ステージ削除」導線からのみ許可すること。
    /// 内部処理・自動化フロー・他コマンドの副作用として無条件に呼び出さないこと。
    /// </summary>
    void DeleteStageFile(string path);
}
