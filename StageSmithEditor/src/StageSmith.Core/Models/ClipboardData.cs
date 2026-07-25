namespace StageSmith.Core.Models;

public record ClipboardData(byte?[,] Tiles)
{
    public int Width => Tiles.GetLength(0);
    public int Height => Tiles.GetLength(1);
}
