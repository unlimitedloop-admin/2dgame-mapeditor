using System.Text.Json.Serialization;

namespace StageSmith.Core.Models;

/// <summary>
/// 敵・アイテムなどの画像シート。プロジェクト全体で共有する。
/// タイル分割はシート定義JSON（{"Loader":{"tileWidth",...}}）から読み込み、値をここにキャッシュする。
/// 定義JSONが見つからない環境でも、キャッシュ値で表示できるようにするため。
/// NOTE: Loader.paletteVariants はゲーム側のフェード段階数なので、エディタでは保持しない。
/// </summary>
public sealed class SpriteSheet
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public string Name { get; set; } = string.Empty;

    /// <summary>シート画像（png）の絶対パス。</summary>
    public string ImagePath { get; set; } = string.Empty;

    /// <summary>シート定義JSONの絶対パス。</summary>
    public string DefinitionPath { get; set; } = string.Empty;

    public int TileWidth { get; set; } = 32;
    public int TileHeight { get; set; } = 32;
    public int TilesX { get; set; } = 1;
    public int TilesY { get; set; } = 1;

    [JsonIgnore]
    public int TileCount => TilesX * TilesY;

    /// <summary>
    /// コマ番号に対応するシート画像上の矩形（x, y, width, height）を返す。
    /// コマ番号は左上から右方向へ 0, 1, 2... と数える。
    /// </summary>
    public (int X, int Y, int Width, int Height) GetTileRect(int tileIndex)
    {
        var columns = Math.Max(1, TilesX);
        return (
            (tileIndex % columns) * TileWidth,
            (tileIndex / columns) * TileHeight,
            TileWidth,
            TileHeight);
    }

    public bool IsValidTileIndex(int tileIndex) => tileIndex >= 0 && tileIndex < TileCount;

    public void Normalize()
    {
        Name ??= string.Empty;
        ImagePath ??= string.Empty;
        DefinitionPath ??= string.Empty;
        TileWidth = Math.Max(1, TileWidth);
        TileHeight = Math.Max(1, TileHeight);
        TilesX = Math.Max(1, TilesX);
        TilesY = Math.Max(1, TilesY);
    }
}
