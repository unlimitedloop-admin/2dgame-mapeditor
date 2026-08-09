using System.Text.Json;
using System.Text.Json.Serialization;
using StageSmith.Core.Models;

namespace StageSmith.Infrastructure;

/// <summary>
/// ステージデータをdefファイル（JSON形式）に出力する。
/// BD-006/BD-007で定義されたスキーマに準拠する。
/// </summary>
public static class DefExporter
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented          = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    /// <summary>
    /// ステージをdefファイルに出力する。
    /// </summary>
    public static void Export(Stage stage, string filePath)
    {
        var root = BuildDefRoot(stage);
        var json = JsonSerializer.Serialize(root, _jsonOptions);
        File.WriteAllText(filePath, json);
    }

    private static DefRoot BuildDefRoot(Stage stage)
    {
        return new DefRoot
        {
            Version = "1.0",
            Stage   = new DefStage
            {
                Id   = stage.StageNumber,
                Name = stage.Name,
            },
            Nodes = [.. stage.Pages.Select(BuildDefNode)],
        };
    }

    private static DefNode BuildDefNode(Page page)
    {
        var h = page.Header;

        return new DefNode
        {
            RoomId  = h.RoomId,
            X       = page.NodeX,
            Y       = page.NodeY,
            Z       = h.Z,
            Enabled = page.Enable,
            Remarks = string.IsNullOrEmpty(page.Remarks) ? null : page.Remarks,
        };
    }

    //========================
    // defファイルスキーマ定義（DefExporterのネストクラス）
    // 外部からは参照不可。DefExporter内部でのみ使用する。
    //========================

    private sealed class DefRoot
    {
        [JsonPropertyName("version")]
        public string Version { get; set; } = "1.0";

        [JsonPropertyName("stage")]
        public DefStage Stage { get; set; } = new();

        [JsonPropertyName("nodes")]
        public List<DefNode> Nodes { get; set; } = [];
    }

    private sealed class DefStage
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = "";
    }

    private sealed class DefNode
    {
        [JsonPropertyName("roomId")]
        public byte RoomId { get; set; }

        [JsonPropertyName("x")]
        public int X { get; set; }

        [JsonPropertyName("y")]
        public int Y { get; set; }

        [JsonPropertyName("z")]
        public int Z { get; set; }

        [JsonPropertyName("enabled")]
        public bool Enabled { get; set; } = true;

        [JsonPropertyName("remarks")]
        public string? Remarks { get; set; }
    }
}
