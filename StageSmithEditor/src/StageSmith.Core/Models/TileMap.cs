using StageSmith.Core.Constants;
using System.Text.Json.Serialization;

namespace StageSmith.Core.Models;

public sealed class TileMap
{
    public int Width => MapConstants.PageTileWidth;
    public int Height => MapConstants.PageTileHeight;

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
        return Tiles[y * Width + x];
    }

    public void SetTile(int x, int y, byte tileId)
    {
        Tiles[y * Width + x] = tileId;
    }

    public byte[] ToArray()
    {
        return (byte[])Tiles.Clone();
    }

    public void Fill(byte tileId)
    {
        Array.Fill(Tiles, tileId);
    }

    public void Clear()
    {
        Fill(0);
    }

    private int ToIndex(int x, int y)
    {
        return y * Width + x;
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
