using StageSmith.Core.Constants;
using System.Text.Json.Serialization;

namespace StageSmith.Core.Models;

public sealed class TileMap
{
    public int Width { get; } = MapConstants.PageTileWidth;
    public int Height { get; } = MapConstants.PageTileHeight;

    [JsonIgnore]
    public byte[] Tiles { get; set; }

    [JsonPropertyName("Tiles")]
    public int[] TilesForJson
    {
        get => Tiles.Select(b => (int)b).ToArray();
        set => Tiles = value.Select(i => (byte)i).ToArray();
    }

    public TileMap()
    {
        Tiles = new byte[Width * Height];
    }

    public byte GetTile(int x, int y)
    {
        if (x < 0 || x >= Width || y < 0 || y >= Height)
            return 0;

        return Tiles[y * Width + x];
    }

    public void SetTile(int x, int y, byte tileId)
    {
        ValidateCoordinates(x, y); // 書き込みは例外で検知
        Tiles[y * Width + x] = tileId;
    }

    public byte[] ToArray()
    {
        return (byte[])Tiles.Clone();
    }

    /// <summary>
    /// この TileMap のディープコピーを返す。
    /// Tiles 配列は新しい配列としてコピーされる。
    /// </summary>
    public TileMap Clone()
    {
        var clone = new TileMap();
        Array.Copy(Tiles, clone.Tiles, Tiles.Length);
        return clone;
    }

    public void Fill(byte tileId)
    {
        Array.Fill(Tiles, tileId);
    }

    public void Clear()
    {
        Fill(0);
    }

    // TODO: もし頻繁にアクセスするなら、座標からインデックスへの変換をメソッド化してもいいかも
    private int ToIndex(int x, int y)
    {
        return y * Width + x;
    }

    // デバッグで利用するための2D配列変換。頻繁に呼び出すものではない想定
    public byte[,] To2DArray()
    {
        var result = new byte[Height, Width];

        for (var y = 0; y < Height; y++)
        {
            for (var x = 0; x < Width; x++)
            {
                result[y, x] = GetTile(x, y);
            }
        }

        return result;
    }

    private void ValidateCoordinates(int x, int y)
    {
        if (x < 0 || x >= Width)
        {
            throw new ArgumentOutOfRangeException(nameof(x));
        }

        if (y < 0 || y >= Height)
        {
            throw new ArgumentOutOfRangeException(nameof(y));
        }
    }
}
