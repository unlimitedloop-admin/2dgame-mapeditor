using StageSmith.Core.Models;

namespace StageSmith.Application.Services;

/// <summary>
/// プロジェクトの永続化を担当するリポジトリインターフェース
/// </summary>
public interface IProjectRepository
{
    /// <summary>
    /// 指定されたパスからプロジェクトを読み込む
    /// </summary>
    /// <param name="path">JSONファイルのパス</param>
    /// <returns>復元されたプロジェクト</returns>
    EditorProject Load(string path);

    /// <summary>
    /// プロジェクトを指定されたパスに保存する
    /// </summary>
    /// <param name="project">保存するプロジェクト</param>
    /// <param name="path">保存先JSONファイルのパス</param>
    void Save(EditorProject project, string path);
}
