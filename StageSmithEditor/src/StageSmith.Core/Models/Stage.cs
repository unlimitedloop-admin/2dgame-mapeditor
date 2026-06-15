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

    public List<Page> Pages { get; set; } = new();

    public Page AddPage(string? name = null)
    {
        // 既存ページの最大NodeXの右隣に配置する。
        // ページが0件のときは原点(0, 0)に配置する。
        var nextX = Pages.Count > 0
            ? Pages.Max(p => p.NodeX) + 1
            : 0;

        var page = new Page
        {
            Name  = name ?? $"Page {Pages.Count:D3}",
            NodeX = nextX,
            NodeY = 0,
        };

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

    /// <summary>
    /// このステージの複製を生成する。
    /// Id は新規発行、Pages は各ページごとにディープコピーされる。
    /// </summary>
    public Stage Clone()
    {
        var clone = new Stage
        {
            Name = Name,
            StageNumber = StageNumber,
            Key = Key,
            TilesetImagePath = TilesetImagePath
        };

        foreach (var page in Pages)
        {
            clone.Pages.Add(page.Clone());
        }

        return clone;
    }

    public byte[] ExportBin()
    {
        var bytes = new List<byte>();

        foreach (var page in Pages)
        {
            bytes.AddRange(page.ToBinary());
        }

        return bytes.ToArray();
    }
}
