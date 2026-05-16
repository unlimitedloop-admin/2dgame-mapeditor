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
    /// 将来の .def 出力時に使う表示名・識別子
    /// </summary>
    public string Key { get; set; } = string.Empty;

    public string TilesetImagePath { get; set; } = string.Empty;

    public List<Page> Pages { get; set; } = new();

    public Page AddPage(string? name = null)
    {
        var page = new Page
        {
            Name = name ?? $"Page {Pages.Count:D3}"
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
