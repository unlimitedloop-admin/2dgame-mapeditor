using System.Globalization;
using System.Text.Json;
using StageSmith.Core.Models;

namespace StageSmith.Infrastructure;

/// <summary>
/// ゲーム側の敵定義フォルダ（assets/data/enemies/*.json）と NES パレット（assets/system/nes_palette.txt）を読み込む。
/// 解釈はゲーム側 EnemyDefinitionLoader / NESPalette に合わせる（エディタが使う項目のみ）。
/// 個々のファイルの問題は Errors に記録して読み飛ばし、読み込み自体は止めない。
/// </summary>
public static class EnemyDefinitionCatalogLoader
{
    private const int MaxNesColorIndex = NesPalette.ColorCount - 1;

    /// <summary>
    /// 敵定義フォルダの位置から nes_palette.txt の場所を推測する（assets/data/enemies → assets/system）。
    /// 見つからなければ null。
    /// </summary>
    public static string? GuessNesPalettePath(string enemyDefinitionDirectory)
    {
        if (string.IsNullOrWhiteSpace(enemyDefinitionDirectory)) return null;

        var candidate = Path.GetFullPath(Path.Combine(enemyDefinitionDirectory, "..", "..", "system", "nes_palette.txt"));
        return File.Exists(candidate) ? candidate : null;
    }

    public static EnemyDefinitionCatalog Load(string? enemyDefinitionDirectory, string? nesPalettePath)
    {
        var catalog = new EnemyDefinitionCatalog();

        if (!string.IsNullOrWhiteSpace(nesPalettePath))
            catalog.NesPalette = LoadNesPalette(nesPalettePath, catalog.Errors);

        if (!string.IsNullOrWhiteSpace(enemyDefinitionDirectory))
            LoadDefinitions(enemyDefinitionDirectory, catalog);

        return catalog;
    }

    //========================
    // NES パレット
    //========================

    private static NesPalette? LoadNesPalette(string path, List<string> errors)
    {
        if (!File.Exists(path))
        {
            errors.Add($"NES パレットが見つかりません: {path}");
            return null;
        }

        var palette = new NesPalette();

        try
        {
            // 形式: "[PaletteNo] [Red] [Green] [Blue]"、# で始まる行はコメント（ゲーム側 NESPalette と同じ）
            foreach (var rawLine in File.ReadLines(path))
            {
                var line = rawLine.Trim();
                if (line.Length == 0 || line[0] == '#') continue;

                var parts = line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 4) continue;

                if (TryParseInt(parts[0], out var index) &&
                    TryParseByte(parts[1], out var r) &&
                    TryParseByte(parts[2], out var g) &&
                    TryParseByte(parts[3], out var b))
                {
                    palette.Set(index, new NesColor(r, g, b));
                }
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            errors.Add($"NES パレットを読み込めません: {path}\n{ex.Message}");
            return null;
        }

        return palette;
    }

    //========================
    // 敵定義
    //========================

    private static void LoadDefinitions(string directory, EnemyDefinitionCatalog catalog)
    {
        if (!Directory.Exists(directory))
        {
            catalog.Errors.Add($"敵定義フォルダが見つかりません: {directory}");
            return;
        }

        // 列挙自体も失敗しうる（アクセス権が無い、ネットワークドライブが切断中など）。
        // プロジェクトを開けなくならないよう、個々のファイルと同じく問題として記録して続行する。
        List<string> files;
        try
        {
            files = [.. Directory.EnumerateFiles(directory, "*.json").OrderBy(f => f, StringComparer.OrdinalIgnoreCase)];
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Security.SecurityException)
        {
            catalog.Errors.Add($"敵定義フォルダを読み込めません: {directory}\n{ex.Message}");
            return;
        }

        foreach (var file in files)
        {
            var name = Path.GetFileName(file);

            EnemyDefinitionInfo? definition;
            try
            {
                using var document = JsonDocument.Parse(File.ReadAllText(file), new JsonDocumentOptions
                {
                    CommentHandling = JsonCommentHandling.Skip,
                    AllowTrailingCommas = true,
                });
                definition = ParseDefinition(document.RootElement, file, out var problem);

                if (definition == null)
                {
                    catalog.Errors.Add($"{name}: {problem}");
                    continue;
                }
            }
            catch (JsonException ex)
            {
                catalog.Errors.Add($"{name}: JSON の形式が不正です（{ex.Message}）");
                continue;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                catalog.Errors.Add($"{name}: 読み込めません（{ex.Message}）");
                continue;
            }

            if (!catalog.TryAdd(definition))
                catalog.Errors.Add($"{name}: id「{definition.Id}」が他のファイルと重複しているため無視しました。");
        }
    }

    private static EnemyDefinitionInfo? ParseDefinition(JsonElement root, string filePath, out string problem)
    {
        problem = string.Empty;

        if (root.ValueKind != JsonValueKind.Object ||
            !root.TryGetProperty("id", out var idElement) ||
            idElement.ValueKind != JsonValueKind.String ||
            string.IsNullOrWhiteSpace(idElement.GetString()))
        {
            problem = "\"id\" がありません（敵定義ではないファイルの可能性があります）。";
            return null;
        }

        var presets = new List<EnemyPalettePreset>();

        if (root.TryGetProperty("palette_presets", out var presetsElement))
        {
            if (presetsElement.ValueKind != JsonValueKind.Array)
            {
                problem = "palette_presets が配列ではありません。";
                return null;
            }

            foreach (var presetElement in presetsElement.EnumerateArray())
            {
                if (!TryParsePreset(presetElement, out var preset, out problem))
                    return null;

                if (presets.Any(p => p.Id == preset.Id))
                {
                    problem = $"palette_presets の id「{preset.Id}」が重複しています。";
                    return null;
                }

                presets.Add(preset);
            }
        }

        return new EnemyDefinitionInfo
        {
            Id = idElement.GetString()!,
            Name = root.TryGetProperty("name", out var nameElement) && nameElement.ValueKind == JsonValueKind.String
                ? nameElement.GetString() ?? string.Empty
                : string.Empty,
            FilePath = filePath,
            SpriteHalfSize = ReadSpriteHalfSize(root),
            PalettePresets = presets,
        };
    }

    private static bool TryParsePreset(JsonElement element, out EnemyPalettePreset preset, out string problem)
    {
        preset = new EnemyPalettePreset();
        problem = string.Empty;

        if (element.ValueKind != JsonValueKind.Object ||
            !element.TryGetProperty("id", out var idElement) ||
            idElement.ValueKind != JsonValueKind.String ||
            string.IsNullOrEmpty(idElement.GetString()))
        {
            problem = "palette_presets に id の無い要素があります。";
            return false;
        }

        var mappings = new List<EnemyPaletteMapping>();

        if (element.TryGetProperty("mappings", out var mappingsElement))
        {
            if (mappingsElement.ValueKind != JsonValueKind.Array)
            {
                problem = $"palette_presets「{idElement.GetString()}」の mappings が配列ではありません。";
                return false;
            }

            foreach (var mapping in mappingsElement.EnumerateArray())
            {
                if (!TryReadColorIndex(mapping, "source", out var source) ||
                    !TryReadColorIndex(mapping, "target", out var target))
                {
                    problem = $"palette_presets「{idElement.GetString()}」の mappings に不正な要素があります（source / target は 0〜{MaxNesColorIndex}）。";
                    return false;
                }

                mappings.Add(new EnemyPaletteMapping(source, target));
            }
        }

        preset = new EnemyPalettePreset { Id = idElement.GetString()!, Mappings = mappings };
        return true;
    }

    private static (double X, double Y)? ReadSpriteHalfSize(JsonElement root)
    {
        if (root.TryGetProperty("abilities", out var abilities) &&
            abilities.ValueKind == JsonValueKind.Object &&
            abilities.TryGetProperty("sprite_half_size", out var half) &&
            half.ValueKind == JsonValueKind.Object &&
            half.TryGetProperty("x", out var x) && x.TryGetDouble(out var hx) &&
            half.TryGetProperty("y", out var y) && y.TryGetDouble(out var hy))
        {
            return (hx, hy);
        }

        return null;
    }

    private static bool TryReadColorIndex(JsonElement element, string name, out int value)
    {
        value = 0;
        return element.ValueKind == JsonValueKind.Object &&
               element.TryGetProperty(name, out var property) &&
               property.ValueKind == JsonValueKind.Number &&
               property.TryGetInt32(out value) &&
               value is >= 0 and <= MaxNesColorIndex;
    }

    private static bool TryParseInt(string text, out int value)
        => int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);

    private static bool TryParseByte(string text, out byte value)
        => byte.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
}
