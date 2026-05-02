namespace StageSmith.Core.Models;

public sealed class Page
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public string Name { get; set; } = "New Page";

    /// <summary>
    /// 後でノード接続や構造ビューアで使うためのラベル
    /// </summary>
    public string Tag { get; set; } = string.Empty;

    /// <summary>
    /// Zレイヤー。現状は整数で十分
    /// </summary>
    public int Z { get; set; } = 0;

    /// <summary>
    /// スクロール属性などは後で拡張
    /// </summary>
    public string ScrollType { get; set; } = "None";

    public TileMap TileMap { get; set; } = new();

    public int Width => TileMap.Width;
    public int Height => TileMap.Height;
}
