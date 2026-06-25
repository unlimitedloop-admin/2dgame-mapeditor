namespace StageSmith.Core.Models;

public sealed class MetaTile
{
    public const byte EmptyTile = 0xFF;

    public Guid Id { get; init; } = Guid.NewGuid();

    public string Name { get; set; } = "New MetaTile";

    public int Width { get; private set; }
    public int Height { get; private set; }

    public byte[] Tiles { get; private set; } = [];

    public MetaTile(int width = 4, int height = 4)
    {
        Resize(width, height);
    }

    public byte GetTile(int x, int y)
    {
        if (x < 0 || x >= Width || y < 0 || y >= Height)
            return EmptyTile;

        return Tiles[y * Width + x];
    }

    public void SetTile(int x, int y, byte tileId)
    {
        if (x < 0 || x >= Width || y < 0 || y >= Height)
            return;

        Tiles[y * Width + x] = tileId;
    }

    public void ClearTile(int x, int y)
    {
        SetTile(x, y, EmptyTile);
    }

    public void Clear()
    {
        Array.Fill(Tiles, EmptyTile);
    }

    public void Resize(int width, int height)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);

        var oldTiles = Tiles;
        var oldWidth = Width;
        var oldHeight = Height;

        Width = width;
        Height = height;

        Tiles = new byte[Width * Height];
        Array.Fill(Tiles, EmptyTile);

        var copyWidth = Math.Min(oldWidth, Width);
        var copyHeight = Math.Min(oldHeight, Height);

        for (var y = 0; y < copyHeight; y++)
        {
            for (var x = 0; x < copyWidth; x++)
            {
                Tiles[y * Width + x] = oldTiles[y * oldWidth + x];
            }
        }
    }
}
