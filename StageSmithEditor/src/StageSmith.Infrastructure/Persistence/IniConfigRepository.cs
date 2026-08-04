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

        config.ShowGridLines = GetBool(values, nameof(config.ShowGridLines), config.ShowGridLines);
        config.ShowTilePreview = GetBool(values, nameof(config.ShowTilePreview), config.ShowTilePreview);
        config.ShowTileNumbers = GetBool(values, nameof(config.ShowTileNumbers), config.ShowTileNumbers);
        config.ShowRowNumbers = GetBool(values, nameof(config.ShowRowNumbers), config.ShowRowNumbers);
        config.ShowColumnNumbers = GetBool(values, nameof(config.ShowColumnNumbers), config.ShowColumnNumbers);
        config.ShowTileInfo = GetBool(values, nameof(config.ShowTileInfo), config.ShowTileInfo);
        config.ShowMarkerOverlay = GetBool(values, nameof(config.ShowMarkerOverlay), config.ShowMarkerOverlay);
        config.ShowToolBar = GetBool(values, nameof(config.ShowToolBar), config.ShowToolBar);
        config.ShowStatusBar = GetBool(values, nameof(config.ShowStatusBar), config.ShowStatusBar);

        config.NumberDisplayFormat = GetEnum(values, nameof(config.NumberDisplayFormat), config.NumberDisplayFormat);
        config.KeepSelectedTileOnPageChange = GetBool(values, nameof(config.KeepSelectedTileOnPageChange), config.KeepSelectedTileOnPageChange);
        config.UseStageSubFolder = GetBool(values, nameof(config.UseStageSubFolder), config.UseStageSubFolder);
        config.UseProjectSubDirectory = GetBool(values, nameof(config.UseProjectSubDirectory), config.UseProjectSubDirectory);
        config.DefaultProjectSaveDirectory = GetString(values, nameof(config.DefaultProjectSaveDirectory), config.DefaultProjectSaveDirectory);
        config.DefaultClearTileId = (byte)GetInt(values, nameof(config.DefaultClearTileId), config.DefaultClearTileId);
        config.UseStageDirectoryForExport = GetBool(values, nameof(config.UseStageDirectoryForExport), config.UseStageDirectoryForExport);

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
            "[View]",
            $"{nameof(config.ShowGridLines)}={config.ShowGridLines}",
            $"{nameof(config.ShowTilePreview)}={config.ShowTilePreview}",
            $"{nameof(config.ShowTileNumbers)}={config.ShowTileNumbers}",
            $"{nameof(config.ShowRowNumbers)}={config.ShowRowNumbers}",
            $"{nameof(config.ShowColumnNumbers)}={config.ShowColumnNumbers}",
            $"{nameof(config.ShowTileInfo)}={config.ShowTileInfo}",
            $"{nameof(config.ShowMarkerOverlay)}={config.ShowMarkerOverlay}",
            $"{nameof(config.ShowToolBar)}={config.ShowToolBar}",
            $"{nameof(config.ShowStatusBar)}={config.ShowStatusBar}",
            "",
            "[Properties]",
            $"{nameof(config.NumberDisplayFormat)}={config.NumberDisplayFormat}",
            $"{nameof(config.KeepSelectedTileOnPageChange)}={config.KeepSelectedTileOnPageChange}",
            $"{nameof(config.UseStageSubFolder)}={config.UseStageSubFolder}",
            $"{nameof(config.UseProjectSubDirectory)}={config.UseProjectSubDirectory}",
            $"{nameof(config.DefaultProjectSaveDirectory)}={config.DefaultProjectSaveDirectory}",
            $"{nameof(config.DefaultClearTileId)}={config.DefaultClearTileId}",
            $"{nameof(config.UseStageDirectoryForExport)}={config.UseStageDirectoryForExport}",
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
