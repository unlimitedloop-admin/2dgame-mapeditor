using StageSmith.Core.Models;

namespace StageSmith.Application.Services;

/// <summary>
/// エディタの設定情報の永続化を担当するリポジトリインターフェース
/// </summary>
public interface IConfigRepository
{
    EditorConfig Load();
    void Save(EditorConfig config);
}
