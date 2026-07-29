using System.Globalization;
using StageSmith.Application.Services;
using StageSmith.Core.Models;

namespace StageSmith.Infrastructure.Persistence;

public class IniConfigRepository : IConfigRepository
{
    private const string DefaultFileName = "SSE_Config.ini";

    private readonly string _filePath;

    public IniConfigRepository(string? filePath = null)
    {
        _filePath = filePath ?? Path.Combine(AppContext.BaseDirectory, DefaultFileName);
    }

    public EditorConfig Load()
    {
        var config = new EditorConfig();

        if (!File.Exists(_filePath))
            return config;

        var values = ReadIniValues(_filePath);

        config.LastOpenedProjectPath = GetString(values, nameof(config.LastOpenedProjectPath), config.LastOpenedProjectPath);
        config.LastSelectedStageIndex = GetInt(values, nameof(config.LastSelectedStageIndex), config.LastSelectedStageIndex);
        config.LastSelectedPageIndex = GetInt(values, nameof(config.LastSelectedPageIndex), config.LastSelectedPageIndex);
        config.LastZ = GetInt(values, nameof(config.LastZ), config.LastZ);
        config.LastToolMode = GetEnum(values, nameof(config.LastToolMode), config.LastToolMode);

        config.MainWindowX = GetInt(values, nameof(config.MainWindowX), config.MainWindowX);
        config.MainWindowY = GetInt(values, nameof(config.MainWindowY), config.MainWindowY);
        config.MainWindowWidth = GetInt(values, nameof(config.MainWindowWidth), config.MainWindowWidth);
        config.MainWindowHeight = GetInt(values, nameof(config.MainWindowHeight), config.MainWindowHeight);
        config.MainWindowMaximized = GetBool(values, nameof(config.MainWindowMaximized), config.MainWindowMaximized);

        config.NumberDisplayFormat = GetEnum(values, nameof(config.NumberDisplayFormat), config.NumberDisplayFormat);
        config.KeepSelectedTileOnPageChange = GetBool(values, nameof(config.KeepSelectedTileOnPageChange), config.KeepSelectedTileOnPageChange);
        config.UseStageSubFolder = GetBool(values, nameof(config.UseStageSubFolder), config.UseStageSubFolder);
        config.UseProjectSubDirectory = GetBool(values, nameof(config.UseProjectSubDirectory), config.UseProjectSubDirectory);
        config.DefaultProjectSaveDirectory = GetString(values, nameof(config.DefaultProjectSaveDirectory), config.DefaultProjectSaveDirectory);
        config.DefaultClearTileId = (byte)GetInt(values, nameof(config.DefaultClearTileId), config.DefaultClearTileId);

        return config;
    }

    public void Save(EditorConfig config)
    {
        var lines = new List<string>
        {
            "[General]",
            $"{nameof(config.LastOpenedProjectPath)}={config.LastOpenedProjectPath}",
            $"{nameof(config.LastSelectedStageIndex)}={config.LastSelectedStageIndex}",
            $"{nameof(config.LastSelectedPageIndex)}={config.LastSelectedPageIndex}",
            $"{nameof(config.LastZ)}={config.LastZ}",
            $"{nameof(config.LastToolMode)}={config.LastToolMode}",
            "",
            "[Window]",
            $"{nameof(config.MainWindowX)}={config.MainWindowX}",
            $"{nameof(config.MainWindowY)}={config.MainWindowY}",
            $"{nameof(config.MainWindowWidth)}={config.MainWindowWidth}",
            $"{nameof(config.MainWindowHeight)}={config.MainWindowHeight}",
            $"{nameof(config.MainWindowMaximized)}={config.MainWindowMaximized}",
            "",
            "[Properties]",
            $"{nameof(config.NumberDisplayFormat)}={config.NumberDisplayFormat}",
            $"{nameof(config.KeepSelectedTileOnPageChange)}={config.KeepSelectedTileOnPageChange}",
            $"{nameof(config.UseStageSubFolder)}={config.UseStageSubFolder}",
            $"{nameof(config.UseProjectSubDirectory)}={config.UseProjectSubDirectory}",
            $"{nameof(config.DefaultProjectSaveDirectory)}={config.DefaultProjectSaveDirectory}",
            $"{nameof(config.DefaultClearTileId)}={config.DefaultClearTileId}",
        };

        File.WriteAllLines(_filePath, lines);
    }

    private static Dictionary<string, string> ReadIniValues(string path)
    {
        var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var rawLine in File.ReadAllLines(path))
        {
            var line = rawLine.Trim();

            if (line.Length == 0 || line.StartsWith(';') || line.StartsWith('#'))
                continue;

            if (line.StartsWith('[') && line.EndsWith(']'))
                continue; // セクション行はスキップ（フラットなキー管理で十分なため）

            var separatorIndex = line.IndexOf('=');
            if (separatorIndex < 0)
                continue;

            var key = line[..separatorIndex].Trim();
            var value = line[(separatorIndex + 1)..].Trim();
            dict[key] = value;
        }

        return dict;
    }

    private static string? GetString(Dictionary<string, string> values, string key, string? fallback)
        => values.TryGetValue(key, out var v) && v.Length > 0 ? v : fallback;

    private static int GetInt(Dictionary<string, string> values, string key, int fallback)
        => values.TryGetValue(key, out var v) &&
           int.TryParse(v, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result)
            ? result
            : fallback;

    private static bool GetBool(Dictionary<string, string> values, string key, bool fallback)
        => values.TryGetValue(key, out var v) && bool.TryParse(v, out var result) ? result : fallback;

    private static TEnum GetEnum<TEnum>(Dictionary<string, string> values, string key, TEnum fallback)
        where TEnum : struct, Enum
        => values.TryGetValue(key, out var v) && Enum.TryParse<TEnum>(v, true, out var result) ? result : fallback;
}
