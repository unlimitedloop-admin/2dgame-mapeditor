using StageSmith.Core.Constants;

namespace StageSmith.Core.Models;

/// <summary>
/// ステージの1ページ分のデータ。
/// </summary>
public sealed class Page
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public string Name { get; set; } = "New Page";

    public bool Enable { get; set; } = true;
    public string Remarks { get; set; } = "";

    /// <summary>付与されているTag.Idの一覧。マスターはEditorProject.Tagsが持つ。</summary>
    public List<string> TagIds { get; set; } = [];

    /// <summary>
    /// ノードエディタ上のX配置座標。エディタ専用（bin/def出力対象外）。
    /// </summary>
    public int NodeX { get; set; } = 0;

    /// <summary>
    /// ノードエディタ上のY配置座標。エディタ専用（bin/def出力対象外）。
    /// </summary>
    public int NodeY { get; set; } = 0;

    public TileMap TileMap { get; set; } = new();

    public int Width => TileMap.Width;
    public int Height => TileMap.Height;

    public PageHeader Header { get; set; } = PageHeader.CreateDefault();

    /// <summary>
    /// この部屋の敵リスポーン既定値（.def の nodes[].enemyRespawn）。
    /// EntityConstants.RespawnValues のいずれか。null なら出力しない（ゲーム側既定）。
    /// </summary>
    public string? EnemyRespawn { get; set; }

    /// <summary>
    /// この部屋に配置されたエンティティ（.def の entities[] のうち、この部屋に属するもの）。
    /// </summary>
    public List<EntityPlacement> Entities { get; set; } = [];

    public EntityPlacement? FindEntity(Guid entityId)
    {
        return Entities.FirstOrDefault(x => x.Id == entityId);
    }

    public void Normalize()
    {
        TagIds ??= [];
        Entities ??= [];

        if (string.IsNullOrWhiteSpace(EnemyRespawn)) EnemyRespawn = null;

        foreach (var entity in Entities)
        {
            entity.Normalize();
        }
    }

    /// <summary>
    /// このページの複製を生成する。
    /// Id は新規発行、TileMap・配置エンティティはディープコピーされる。
    /// NOTE: エンティティの EntityId はそのまま複製されるため、同一ステージ内へ追加する場合は
    /// 呼び出し側で Stage.EnsureUniqueEntityIds() を呼んで採番し直すこと。
    /// </summary>
    public Page Clone()
    {
        var clone = new Page
        {
            Name = Name,
            Enable = Enable,
            Remarks = Remarks,
            TagIds = [.. TagIds],
            NodeX = NodeX,
            NodeY = NodeY,
            Header = Header.Clone(),
            TileMap = TileMap.Clone(),
            EnemyRespawn = EnemyRespawn,
            Entities = [.. Entities.Select(e => e.Clone())]
        };

        return clone;
    }

    public byte[] ToBinary()
    {
        var buffer = new byte[MapConstants.PageSize];

        var headerBytes = Header.ToBytes();
        if (headerBytes.Length != PageHeader.Size)
        {
            throw new InvalidOperationException($"PageHeader must be {PageHeader.Size} bytes, but was {headerBytes.Length} bytes.");
        }

        Array.Copy(headerBytes, 0, buffer, 0x00, PageHeader.Size);

        // NOTE: indexをPageHeader.Sizeから始めることで、ヘッダーの後にタイルデータを書き込む
        var index = PageHeader.Size;

        for (var y = 0; y < TileMap.Height; y++)
        {
            for (var x = 0; x < TileMap.Width; x++)
            {
                buffer[index++] = TileMap.GetTile(x, y);
            }
        }

        return buffer;
    }
}
