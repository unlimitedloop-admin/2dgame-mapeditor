namespace StageSmith.Core.Models;

public sealed class Stage
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public string Name { get; set; } = "New Stage";

    /// <summary>
    /// 表示順・出力順を意識した番号
    /// </summary>
    public int StageNumber { get; set; }

    /// <summary>
    /// ステージの説明。将来のエディタで表示するためのもの。
    /// .def 出力には使わない。
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 将来の .def 出力時に使う表示名・識別子
    /// </summary>
    public string Key { get; set; } = string.Empty;

    public string TilesetImagePath { get; set; } = string.Empty;

    public List<Page> Pages { get; set; } = [];

    /// <summary>
    /// ステージ単位で管理するメタタイル定義。
    /// .sseproj の保存対象にする想定。
    /// </summary>
    public List<MetaTile> MetaTiles { get; set; } = [];

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

        foreach (var metaTile in MetaTiles)
        {
            metaTile.Normalize();
        }

        NormalizeMetaTileIds();
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
    /// Id は新規発行、Pages / MetaTiles はディープコピーされる。
    /// </summary>
    public Stage Clone()
    {
        var clone = new Stage
        {
            Name = Name,
            StageNumber = StageNumber,
            Description = Description,
            Key = Key,
            TilesetImagePath = TilesetImagePath
        };

        foreach (var page in Pages)
        {
            clone.Pages.Add(page.Clone());
        }

        foreach (var metaTile in MetaTiles)
        {
            clone.MetaTiles.Add(metaTile.Clone(keepId: true));
        }

        return clone;
    }

    private byte GetNextAvailableRoomId()
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
