using System.Text.Json;
using StageSmith.Core.Models;

namespace StageSmith.Infrastructure;

/// <summary>
/// 画像シートの分割定義JSONを読み込む。
/// 形式はゲーム側と共通:
/// <code>{"Loader":{"tileWidth":32,"tileHeight":32,"tilesX":4,"tilesY":6,"paletteVariants":4}}</code>
/// NOTE: paletteVariants はゲーム側のフェード段階数なのでエディタでは読まない。
/// </summary>
public static class SpriteSheetDefinitionLoader
{
    public const string DefinitionExtension = ".json";

    /// <summary>
    /// 画像と同じフォルダ・同じファイル名の .json があればそのパスを返す。
    /// （例: METALL_ARMY_N0_ALL_PATTERN.png → METALL_ARMY_N0_ALL_PATTERN.json）
    /// </summary>
    public static string? FindDefinitionFor(string imagePath)
    {
        var candidate = Path.ChangeExtension(imagePath, DefinitionExtension);
        return File.Exists(candidate) ? candidate : null;
    }

    /// <summary>
    /// 定義JSONを読み込み、シートの分割値を反映した SpriteSheet を新規作成する。
    /// </summary>
    /// <exception cref="InvalidDataException">定義の形式が不正な場合。</exception>
    public static SpriteSheet Create(string imagePath, string definitionPath)
    {
        var sheet = new SpriteSheet
        {
            Name           = Path.GetFileNameWithoutExtension(imagePath),
            ImagePath      = imagePath,
            DefinitionPath = definitionPath,
        };

        Apply(sheet, Read(definitionPath));
        return sheet;
    }

    /// <summary>
    /// 定義JSONを読み直し、キャッシュしている分割値を更新する。
    /// 定義ファイルが見つからない・壊れている場合は何もせず false を返す（キャッシュ値のまま使う）。
    /// </summary>
    /// <returns>分割値が変化した場合 true。</returns>
    public static bool TryRefresh(SpriteSheet sheet)
    {
        if (string.IsNullOrWhiteSpace(sheet.DefinitionPath) || !File.Exists(sheet.DefinitionPath))
            return false;

        LoaderValues values;
        try
        {
            values = Read(sheet.DefinitionPath);
        }
        catch (Exception ex) when (ex is IOException or InvalidDataException or UnauthorizedAccessException)
        {
            return false;
        }

        var changed = sheet.TileWidth != values.TileWidth
                   || sheet.TileHeight != values.TileHeight
                   || sheet.TilesX != values.TilesX
                   || sheet.TilesY != values.TilesY;

        Apply(sheet, values);
        return changed;
    }

    private static void Apply(SpriteSheet sheet, LoaderValues values)
    {
        sheet.TileWidth  = values.TileWidth;
        sheet.TileHeight = values.TileHeight;
        sheet.TilesX     = values.TilesX;
        sheet.TilesY     = values.TilesY;
    }

    private static LoaderValues Read(string definitionPath)
    {
        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(File.ReadAllText(definitionPath));
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException($"JSON の形式が不正です。\n{ex.Message}", ex);
        }

        using (document)
        {
            if (!TryGetPropertyIgnoreCase(document.RootElement, "Loader", out var loader) ||
                loader.ValueKind != JsonValueKind.Object)
            {
                throw new InvalidDataException("\"Loader\" オブジェクトがありません。");
            }

            return new LoaderValues(
                ReadPositiveInt(loader, "tileWidth"),
                ReadPositiveInt(loader, "tileHeight"),
                ReadPositiveInt(loader, "tilesX"),
                ReadPositiveInt(loader, "tilesY"));
        }
    }

    private static int ReadPositiveInt(JsonElement loader, string name)
    {
        if (!TryGetPropertyIgnoreCase(loader, name, out var value) ||
            value.ValueKind != JsonValueKind.Number ||
            !value.TryGetInt32(out var number) ||
            number <= 0)
        {
            throw new InvalidDataException($"Loader.{name} には 1 以上の整数が必要です。");
        }

        return number;
    }

    private static bool TryGetPropertyIgnoreCase(JsonElement element, string name, out JsonElement value)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase))
                {
                    value = property.Value;
                    return true;
                }
            }
        }

        value = default;
        return false;
    }

    private readonly record struct LoaderValues(int TileWidth, int TileHeight, int TilesX, int TilesY);
}
