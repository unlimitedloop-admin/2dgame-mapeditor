namespace StageSmith.Core.Models;

/// <summary>
/// クリップボードにコピーされたタイルデータを表すレコード。
/// マップビュー上でコピーされたタイルの範囲を保持するために使用される。
/// </summary>
/// <param name="Tiles">タイルデータの2次元配列。</param>
public record ClipboardData(byte?[,] Tiles)
{
    public int Width => Tiles.GetLength(0);
    public int Height => Tiles.GetLength(1);
}
