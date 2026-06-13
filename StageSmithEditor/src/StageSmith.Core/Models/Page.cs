using StageSmith.Core.Constants;

namespace StageSmith.Core.Models;

public sealed class Page
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public string Name { get; set; } = "New Page";

    public bool Enable { get; set; } = true;
    public bool ReadOnly { get; set; }
    public string Remarks { get; set; } = "";

    /// <summary>
    /// 後でノード接続や構造ビューアで使うためのラベル
    /// </summary>
    public string Tag { get; set; } = string.Empty;

    /// <summary>
    /// ノードエディタ上のX配置座標。エディタ専用（bin/def出力対象外）。
    /// </summary>
    public int NodeX { get; set; } = 0;

    /// <summary>
    /// ノードエディタ上のY配置座標。エディタ専用（bin/def出力対象外）。
    /// </summary>
    public int NodeY { get; set; } = 0;

    /// <summary>
    /// スクロール属性などは後で拡張
    /// </summary>
    public string ScrollType { get; set; } = "None";

    public TileMap TileMap { get; set; } = new();

    public int Width => TileMap.Width;
    public int Height => TileMap.Height;

    public PageHeader Header { get; set; } = PageHeader.CreateDefault();

    /// <summary>
    /// このページの複製を生成する。
    /// Id は新規発行、TileMap はディープコピーされる。
    /// </summary>
    public Page Clone()
    {
        var clone = new Page
        {
            Name = Name,
            Enable = Enable,
            ReadOnly = ReadOnly,
            Remarks = Remarks,
            Tag = Tag,
            NodeX = NodeX,
            NodeY = NodeY,
            ScrollType = ScrollType,
            Header = Header.Clone(),
            TileMap = TileMap.Clone()
        };

        return clone;
    }

    public byte[] ToBinary()
    {
        // ヘッダ16バイト + タイルデータ(幅×高さ)
        // ページは最大16x15なので、合計256バイトで固定
        var buffer = new byte[0x100];

        var headerBytes = Header.ToBytes();
        if (headerBytes.Length != 0x10)
        {
            throw new InvalidOperationException($"PageHeader must be 16 bytes, but was {headerBytes.Length} bytes.");
        }

        Array.Copy(headerBytes, 0, buffer, 0x00, 0x10);

        var index = 0x10;

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
