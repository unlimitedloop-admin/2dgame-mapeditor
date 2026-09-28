using StageSmith.Core.Constants;

namespace StageSmith.Core.Models;

/// <summary>
/// ページ（部屋）上に配置されたエンティティ1体分。.def の entities[] の1要素に対応する。
/// 所属部屋は保持している Page で決まり、roomId は .def 出力時に Page.Header.RoomId から解決する。
/// </summary>
public sealed class EntityPlacement
{
    /// <summary>エディタ内部の識別子（Undo・選択の参照用）。.def には出力しない。</summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>.def の id。ステージ内で一意であること。</summary>
    public string EntityId { get; set; } = string.Empty;

    /// <summary>
    /// 描画に使う EntityTemplate の Id。
    /// 値（Kind等）は配置時にコピー済みなので、テンプレートが削除されても出力には影響しない。
    /// </summary>
    public Guid? TemplateId { get; set; }

    public string Type { get; set; } = EntityConstants.TypeEnemy;

    /// <summary>足元中心の部屋内ピクセルX（0〜255）。</summary>
    public int X { get; set; }

    /// <summary>足元中心の部屋内ピクセルY（0〜239）。</summary>
    public int Y { get; set; }

    public EntityProperties Properties { get; set; } = new();

    public void Normalize()
    {
        EntityId ??= string.Empty;
        if (string.IsNullOrWhiteSpace(Type)) Type = EntityConstants.TypeEnemy;
        Properties ??= new EntityProperties();
        Properties.Normalize();
    }

    /// <summary>
    /// 複製を生成する。keepId=false の場合は内部Idを新規発行する（EntityId はそのまま）。
    /// </summary>
    public EntityPlacement Clone(bool keepId = false)
    {
        return new EntityPlacement
        {
            Id         = keepId ? Id : Guid.NewGuid(),
            EntityId   = EntityId,
            TemplateId = TemplateId,
            Type       = Type,
            X          = X,
            Y          = Y,
            Properties = Properties.Clone(),
        };
    }
}
