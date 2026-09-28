using StageSmith.Core.Constants;

namespace StageSmith.Core.Models;

/// <summary>
/// 配置用の画像（シートの1コマ＋エンティティ既定値）。プロジェクト全体で共有する。
/// オブジェクトパレットで選ぶ単位で、配置時に Type / Properties が EntityPlacement へコピーされる。
/// </summary>
public sealed class EntityTemplate
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public string Name { get; set; } = string.Empty;

    public Guid SheetId { get; set; }

    /// <summary>シート内のコマ番号（左上から右方向へ 0 始まり）。</summary>
    public int TileIndex { get; set; }

    public string Type { get; set; } = EntityConstants.TypeEnemy;

    /// <summary>配置時の既定値。Kind は必須。</summary>
    public EntityProperties Properties { get; set; } = new();

    public void Normalize()
    {
        Name ??= string.Empty;
        if (string.IsNullOrWhiteSpace(Type)) Type = EntityConstants.TypeEnemy;
        if (TileIndex < 0) TileIndex = 0;
        Properties ??= new EntityProperties();
        Properties.Normalize();
    }

    /// <summary>
    /// このテンプレートから配置データを生成する（EntityId は呼び出し側で採番する）。
    /// </summary>
    public EntityPlacement CreatePlacement(string entityId, int x, int y)
    {
        return new EntityPlacement
        {
            EntityId   = entityId,
            TemplateId = Id,
            Type       = Type,
            X          = x,
            Y          = y,
            Properties = Properties.Clone(),
        };
    }
}
