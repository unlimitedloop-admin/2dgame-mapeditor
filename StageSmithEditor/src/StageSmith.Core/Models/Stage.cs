using System.Text.Json.Serialization;

namespace StageSmith.Core.Models;

/// <summary>
/// ステージ定義ファイル(.ssestage)の内容を表すクラス。
/// </summary>
public sealed class Stage
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public string Name { get; set; } = "New Stage";

    public int StageNumber { get; set; }

    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 将来の .def 出力時に使う表示名・識別子
    /// </summary>
    public string Key { get; set; } = string.Empty;

    public string TilesetImagePath { get; set; } = string.Empty;

    public List<Page> Pages { get; set; } = [];

    public List<MetaTile> MetaTiles { get; set; } = [];

    /// <summary>付与されているTag.Idの一覧。マスターはEditorProject.Tagsが持つ。</summary>
    public List<string> TagIds { get; set; } = [];

    /// <summary>
    /// プレイヤー開始位置（.def の stage.start）。null なら出力しない。
    /// </summary>
    public PlayerStart? PlayerStart { get; set; }

    [JsonIgnore]
    public string? FilePath { get; set; }

    /// <summary>
    /// 前回保存以降に変更があるかどうか。
    /// Save Stage / Save Project の対象判定に使う。
    /// </summary>
    [JsonIgnore]
    public bool IsDirty { get; set; }

    public void MarkDirty() => IsDirty = true;

    public void ClearDirty() => IsDirty = false;

    public Page AddPage(string? name = null)
    {
        // 既存ページの最大NodeXの右隣に配置する。
        // ページが0件のときは原点(0, 0)に配置する。
        var nextX = Pages.Count > 0
            ? Pages.Max(p => p.NodeX) + 1
            : 0;

        var roomId = GetNextAvailableRoomId();

        var page = new Page
        {
            Name  = name ?? $"Page {Pages.Count:D3}",
            NodeX = nextX,
            NodeY = 0,
        };
        
        var header = page.Header;
        header.RoomId = roomId;
        page.Header = header;

        Pages.Add(page);
        return page;
    }

    public bool RemovePage(Guid pageId)
    {
        var target = Pages.FirstOrDefault(x => x.Id == pageId);
        if (target is null)
        {
            return false;
        }

        return Pages.Remove(target);
    }

    public Page? FindPage(Guid pageId)
    {
        return Pages.FirstOrDefault(x => x.Id == pageId);
    }

    public MetaTile AddMetaTile(MetaTile source)
    {
        ArgumentNullException.ThrowIfNull(source);

        var metaTile = source.Clone(keepId: false);
        metaTile.Id = GetNextMetaTileId();

        if (string.IsNullOrWhiteSpace(metaTile.Name))
        {
            metaTile.Name = $"MetaTile {metaTile.Id:D3}";
        }

        MetaTiles.Add(metaTile);
        return metaTile;
    }

    public bool UpdateMetaTile(MetaTile source)
    {
        ArgumentNullException.ThrowIfNull(source);

        var index = MetaTiles.FindIndex(x => x.Id == source.Id);
        if (index < 0)
        {
            return false;
        }

        MetaTiles[index] = source.Clone(keepId: true);
        return true;
    }

    public bool RemoveMetaTile(int metaTileId)
    {
        var target = FindMetaTile(metaTileId);
        if (target is null)
        {
            return false;
        }

        return MetaTiles.Remove(target);
    }

    public MetaTile? FindMetaTile(int metaTileId)
    {
        return MetaTiles.FirstOrDefault(x => x.Id == metaTileId);
    }

    private int GetNextMetaTileId()
    {
        return MetaTiles.Count == 0
            ? 0
            : MetaTiles.Max(x => x.Id) + 1;
    }


    /// <summary>
    /// JSON読込後の安全化処理。
    /// MetaTiles など、古いプロジェクトファイルに存在しない可能性がある項目を補正する。
    /// </summary>
    public void Normalize()
    {
        Pages ??= [];
        MetaTiles ??= [];
        TagIds ??= [];

        foreach (var page in Pages)
        {
            page.Normalize();
        }

        foreach (var metaTile in MetaTiles)
        {
            metaTile.Normalize();
        }

        NormalizeMetaTileIds();
    }

    // =========================
    // エンティティ配置
    // =========================

    /// <summary>
    /// ステージ内の全エンティティを、所属ページと組にしてページ順・配置順で列挙する。
    /// </summary>
    public IEnumerable<(Page Page, EntityPlacement Entity)> EnumerateEntities()
    {
        foreach (var page in Pages)
        {
            foreach (var entity in page.Entities)
            {
                yield return (page, entity);
            }
        }
    }

    /// <summary>
    /// ステージ内で未使用の EntityId を「{kind}_{連番}」形式で採番する。
    /// </summary>
    public string GenerateEntityId(string kind)
    {
        var used = EnumerateEntities()
            .Select(x => x.Entity.EntityId)
            .ToHashSet(StringComparer.Ordinal);

        return GenerateEntityId(kind, used);
    }

    private static string GenerateEntityId(string kind, HashSet<string> used)
    {
        var prefix = string.IsNullOrWhiteSpace(kind) ? "entity" : kind.Trim();

        for (var n = 1; ; n++)
        {
            var candidate = $"{prefix}_{n}";
            if (!used.Contains(candidate))
                return candidate;
        }
    }

    /// <summary>
    /// 指定ページのエンティティのうち、ステージ内の他ページと EntityId が重複するもの・空のものを採番し直す。
    /// ページ複製など、同一ステージ内にエンティティを複製した直後に呼ぶ。
    /// </summary>
    public void EnsureUniqueEntityIds(Page page)
    {
        var used = Pages
            .Where(p => !ReferenceEquals(p, page))
            .SelectMany(p => p.Entities)
            .Select(e => e.EntityId)
            .ToHashSet(StringComparer.Ordinal);

        foreach (var entity in page.Entities)
        {
            if (string.IsNullOrWhiteSpace(entity.EntityId) || used.Contains(entity.EntityId))
            {
                entity.EntityId = GenerateEntityId(entity.Properties.Kind, used);
            }

            used.Add(entity.EntityId);
        }
    }

    private void NormalizeMetaTileIds()
    {
        var usedIds = new HashSet<int>();

        foreach (var metaTile in MetaTiles)
        {
            if (metaTile.Id < 0 || !usedIds.Add(metaTile.Id))
            {
                metaTile.Id = GetNextAvailableMetaTileId(usedIds);
                usedIds.Add(metaTile.Id);
            }
        }
    }

    private static int GetNextAvailableMetaTileId(HashSet<int> usedIds)
    {
        var id = 0;

        while (usedIds.Contains(id))
        {
            id++;
        }

        return id;
    }

    /// <summary>
    /// このステージの複製を生成する。
    /// </summary>
    public Stage Clone()
    {
        var clone = new Stage
        {
            Name = Name,
            StageNumber = StageNumber,
            Description = Description,
            Key = Key,
            TilesetImagePath = TilesetImagePath,
            TagIds = [.. TagIds],
            FilePath = null,   // 複製は未保存扱い
            IsDirty = true      // 保存されるまでダーティ扱い
        };

        foreach (var page in Pages)
        {
            var pageClone = page.Clone();
            clone.Pages.Add(pageClone);

            // Page.Clone() は Id を新規発行するため、開始位置の参照先も複製後のページへ付け替える
            if (PlayerStart != null && PlayerStart.PageId == page.Id)
            {
                clone.PlayerStart = PlayerStart.Clone();
                clone.PlayerStart.PageId = pageClone.Id;
            }
        }

        foreach (var metaTile in MetaTiles)
            clone.MetaTiles.Add(metaTile.Clone(keepId: true));

        return clone;
    }

    public byte GetNextAvailableRoomId()
    {
        var used = Pages
            .Select(p => p.Header.RoomId)
            .Where(id => id != 0xFF)
            .ToHashSet();

        for (var i = 0; i <= byte.MaxValue; i++)
        {
            var id = (byte)i;

            if (id == 0xFF)
                continue;

            if (!used.Contains(id))
                return id;
        }

        throw new InvalidOperationException("利用可能な RoomId がありません。");
    }

    public byte[] ExportBin()
    {
        var bytes = new List<byte>();

        foreach (var page in Pages)
        {
            bytes.AddRange(page.ToBinary());
        }

        return [.. bytes];
    }
}
