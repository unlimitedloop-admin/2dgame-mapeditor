namespace StageSmith.Core.Models;

public sealed class MetaTile
{
    public const byte EmptyTile = 0xFF;

    public int Id { get; set; }

    public string Name { get; set; } = "New MetaTile";

    public int Width { get; set; } = 4;

    public int Height { get; set; } = 4;

    public byte[] Tiles { get; set; } = [];

    public MetaTile()
    {
        Normalize();
    }

    public MetaTile(int width, int height)
    {
        Width = width;
        Height = height;
        Normalize();
    }

    public byte GetTile(int x, int y)
    {
        if (x < 0 || x >= Width || y < 0 || y >= Height)
            return EmptyTile;

        var index = y * Width + x;

        if (index < 0 || index >= Tiles.Length)
            return EmptyTile;

        return Tiles[index];
    }

    public void SetTile(int x, int y, byte tileId)
    {
        if (x < 0 || x >= Width || y < 0 || y >= Height)
            return;

        var index = y * Width + x;

        if (index < 0 || index >= Tiles.Length)
            return;

        Tiles[index] = tileId;
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
                var oldIndex = y * oldWidth + x;
                var newIndex = y * Width + x;

                if (oldIndex < 0 || oldIndex >= oldTiles.Length)
                    continue;

                if (newIndex < 0 || newIndex >= Tiles.Length)
                    continue;

                Tiles[newIndex] = oldTiles[oldIndex];
            }
        }
    }

    /// <summary>
    /// JSON読込後の安全化処理。
    /// Width / Height / Tiles の不整合を補正する。
    /// </summary>
    public void Normalize()
    {
        if (Width <= 0)
            Width = 4;

        if (Height <= 0)
            Height = 4;

        Name ??= string.Empty;
        Tiles ??= [];

        var expectedLength = Width * Height;

        if (expectedLength <= 0)
        {
            Width = 4;
            Height = 4;
            expectedLength = Width * Height;
        }

        if (Tiles.Length == expectedLength)
            return;

        var oldTiles = Tiles;

        Tiles = new byte[expectedLength];
        Array.Fill(Tiles, EmptyTile);

        Array.Copy(
            oldTiles,
            Tiles,
            Math.Min(oldTiles.Length, Tiles.Length)
        );
    }

    public MetaTile Clone(bool keepId = true)
    {
        Normalize();

        var clone = new MetaTile(Width, Height)
        {
            Id = keepId ? Id : 0,
            Name = Name
        };

        Array.Copy(Tiles, clone.Tiles, Tiles.Length);
        return clone;
    }
}
