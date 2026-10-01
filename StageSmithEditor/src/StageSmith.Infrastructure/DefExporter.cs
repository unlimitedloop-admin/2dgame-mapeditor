using System.Text.Json;
using System.Text.Json.Serialization;
using StageSmith.Core.Constants;
using StageSmith.Core.Models;

namespace StageSmith.Infrastructure;

/// <summary>
/// ステージデータをdefファイル（JSON形式）に出力する。
/// BD-006/BD-007で定義されたスキーマに準拠する。
/// stage.start / nodes[].enemyRespawn / entities[] の形はゲーム側 StageDefinitionLoader の実装を正とする。
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
    /// 出力前に Validate() でエラーが無いことを確認しておくこと（エラーがある場合は例外を投げる）。
    /// 警告（CollectWarnings）は出力を妨げない。
    /// </summary>
    public static void Export(Stage stage, string filePath, EnemyDefinitionCatalog? enemyDefinitions = null)
    {
        var errors = Validate(stage, enemyDefinitions);
        if (errors.Count > 0)
        {
            throw new InvalidOperationException(
                $"ステージ「{stage.Name}」の def 出力を中止しました。\n" + string.Join("\n", errors));
        }

        var root = BuildDefRoot(stage);
        var json = JsonSerializer.Serialize(root, _jsonOptions);
        File.WriteAllText(filePath, json);
    }

    //========================
    // 出力前検証
    //========================

    /// <summary>
    /// ゲーム側が読み込みエラーにする内容、またはゲーム内で敵が出現しなくなる内容を出力前に検出する。
    /// 戻り値が空なら出力可能。
    /// enemyDefinitions に敵定義が読み込まれている場合のみ、kind が実在するかも検査する。
    /// </summary>
    public static IReadOnlyList<string> Validate(Stage stage, EnemyDefinitionCatalog? enemyDefinitions = null)
    {
        var errors = new List<string>();

        ValidatePlayerStart(stage, errors);
        ValidateNodes(stage, errors);
        ValidateEntities(stage, errors);
        ValidateRoomIdConflicts(stage, errors);
        ValidateKinds(stage, enemyDefinitions, errors);

        return errors;
    }

    /// <summary>
    /// 出力は可能だが、ゲーム内の結果が意図と異なりそうな内容を検出する。
    /// 現在は「palette がその kind のプリセットに無い」（ゲーム側は先頭のプリセットで代用する）のみ。
    /// </summary>
    public static IReadOnlyList<string> CollectWarnings(Stage stage, EnemyDefinitionCatalog? enemyDefinitions)
    {
        var warnings = new List<string>();
        if (enemyDefinitions?.HasDefinitions != true) return warnings;

        foreach (var (page, entity) in stage.EnumerateEntities())
        {
            if (entity.Type != EntityConstants.TypeEnemy) continue;

            var p = entity.Properties;
            if (enemyDefinitions.IsKnownPalette(p.Kind, p.Palette) != false) continue;

            var fallback = enemyDefinitions.Find(p.Kind)!.PalettePresets[0].Id;
            warnings.Add(
                $"エンティティ「{entity.EntityId}」（ページ「{page.Name}」）: palette「{p.Palette ?? EnemyDefinitionCatalog.DefaultPaletteId}」は " +
                $"kind「{p.Kind}」のプリセットにありません（ゲーム内では「{fallback}」で表示されます）。");
        }

        return warnings;
    }

    /// <summary>
    /// kind に対応する敵定義が無い敵は、ゲーム側で ignored になり出現しない（EnemySpawnDirector）。
    /// </summary>
    private static void ValidateKinds(Stage stage, EnemyDefinitionCatalog? enemyDefinitions, List<string> errors)
    {
        if (enemyDefinitions?.HasDefinitions != true) return;

        foreach (var (page, entity) in stage.EnumerateEntities())
        {
            if (entity.Type != EntityConstants.TypeEnemy) continue;

            var kind = entity.Properties.Kind;
            if (string.IsNullOrWhiteSpace(kind) || enemyDefinitions.Find(kind) != null) continue;

            errors.Add(
                $"エンティティ「{entity.EntityId}」（ページ「{page.Name}」）: kind「{kind}」の敵定義が見つかりません" +
                "（ゲーム内で出現しません）。");
        }
    }

    private static void ValidatePlayerStart(Stage stage, List<string> errors)
    {
        var start = stage.PlayerStart;
        if (start == null) return;

        var page = stage.FindPage(start.PageId);
        if (page == null)
        {
            errors.Add("プレイヤー開始位置: 配置先のページが存在しません（削除された可能性があります）。");
            return;
        }

        if (page.Header.RoomId == EntityConstants.UnassignedRoomId)
            errors.Add($"プレイヤー開始位置: ページ「{page.Name}」の RoomId が未割当（0xFF）です。");

        if (!EntityConstants.IsInsideRoom(start.X, start.Y))
            errors.Add($"プレイヤー開始位置: 座標 ({start.X}, {start.Y}) が部屋の範囲外です。");
    }

    private static void ValidateNodes(Stage stage, List<string> errors)
    {
        foreach (var page in stage.Pages)
        {
            if (page.EnemyRespawn != null && !EntityConstants.RespawnValues.Contains(page.EnemyRespawn))
                errors.Add($"ページ「{page.Name}」: enemyRespawn の値「{page.EnemyRespawn}」が不正です。");
        }
    }

    private static void ValidateEntities(Stage stage, List<string> errors)
    {
        var usedIds = new HashSet<string>(StringComparer.Ordinal);

        foreach (var (page, entity) in stage.EnumerateEntities())
        {
            var label = string.IsNullOrWhiteSpace(entity.EntityId)
                ? $"ページ「{page.Name}」のエンティティ"
                : $"エンティティ「{entity.EntityId}」（ページ「{page.Name}」）";

            if (string.IsNullOrWhiteSpace(entity.EntityId))
                errors.Add($"{label}: id が空です。");
            else if (!usedIds.Add(entity.EntityId))
                errors.Add($"{label}: id がステージ内で重複しています。");

            if (page.Header.RoomId == EntityConstants.UnassignedRoomId)
                errors.Add($"{label}: 配置先ページの RoomId が未割当（0xFF）です。");

            if (!EntityConstants.IsInsideRoom(entity.X, entity.Y))
                errors.Add($"{label}: 座標 ({entity.X}, {entity.Y}) が部屋の範囲外です。");

            var p = entity.Properties;

            if (string.IsNullOrWhiteSpace(p.Kind))
                errors.Add($"{label}: kind が空です。");

            if (p.Facing != null && !EntityConstants.FacingValues.Contains(p.Facing))
                errors.Add($"{label}: facing の値「{p.Facing}」が不正です。");

            if (p.Respawn != null && !EntityConstants.RespawnValues.Contains(p.Respawn))
                errors.Add($"{label}: respawn の値「{p.Respawn}」が不正です。");
        }
    }

    /// <summary>
    /// ゲーム側は roomId で部屋を引くため、配置データ（開始位置・エンティティ・enemyRespawn）を持つ部屋の
    /// RoomId が他ページと重複していると、どの部屋に属するか決まらない。
    /// </summary>
    private static void ValidateRoomIdConflicts(Stage stage, List<string> errors)
    {
        var startPageId = stage.PlayerStart?.PageId;

        var conflicts = stage.Pages
            .Where(p => p.Header.RoomId != EntityConstants.UnassignedRoomId)
            .GroupBy(p => p.Header.RoomId)
            .Where(g => g.Count() > 1)
            .Where(g => g.Any(p => p.Entities.Count > 0 || p.EnemyRespawn != null || p.Id == startPageId));

        foreach (var group in conflicts)
        {
            var names = string.Join("」「", group.Select(p => p.Name));
            errors.Add($"RoomId 0x{group.Key:X2}: 配置データを持つ部屋の RoomId が重複しています（「{names}」）。");
        }
    }

    //========================
    // 構築
    //========================

    /// <summary>
    /// ステージからDefRootオブジェクトを構築する。
    /// </summary>
    private static DefRoot BuildDefRoot(Stage stage)
    {
        return new DefRoot
        {
            Version = "1.0",
            Stage   = new DefStage
            {
                Id    = stage.StageNumber,
                Name  = stage.Name,
                Start = BuildDefStart(stage),
            },
            Nodes    = [.. stage.Pages.Select(BuildDefNode)],
            Entities = [.. stage.EnumerateEntities().Select(x => BuildDefEntity(x.Page, x.Entity))],
        };
    }

    private static DefStart? BuildDefStart(Stage stage)
    {
        var start = stage.PlayerStart;
        if (start == null) return null;

        var page = stage.FindPage(start.PageId);
        if (page == null) return null;

        return new DefStart
        {
            RoomId = page.Header.RoomId,
            X      = start.X,
            Y      = start.Y,
        };
    }

    /// <summary>
    /// ページからDefNodeオブジェクトを構築する。
    /// </summary>
    private static DefNode BuildDefNode(Page page)
    {
        var h = page.Header;

        return new DefNode
        {
            RoomId       = h.RoomId,
            X            = page.NodeX,
            Y            = page.NodeY,
            Z            = h.Z,
            Enabled      = page.Enable,
            Remarks      = string.IsNullOrEmpty(page.Remarks) ? null : page.Remarks,
            EnemyRespawn = page.EnemyRespawn,
        };
    }

    private static DefEntity BuildDefEntity(Page page, EntityPlacement entity)
    {
        var p = entity.Properties;

        return new DefEntity
        {
            Type     = entity.Type,
            Id       = entity.EntityId,
            RoomId   = page.Header.RoomId,
            Position = new DefPosition { X = entity.X, Y = entity.Y },
            Properties = new DefEntityProperties
            {
                Kind             = p.Kind,
                Palette          = p.Palette,
                Facing           = p.Facing,
                Respawn          = p.Respawn,
                DespawnOffscreen = p.DespawnOffscreen,
            },
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

        [JsonPropertyName("entities")]
        public List<DefEntity> Entities { get; set; } = [];
    }

    private sealed class DefStage
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = "";

        [JsonPropertyName("start")]
        public DefStart? Start { get; set; }
    }

    private sealed class DefStart
    {
        [JsonPropertyName("roomId")]
        public byte RoomId { get; set; }

        [JsonPropertyName("x")]
        public int X { get; set; }

        [JsonPropertyName("y")]
        public int Y { get; set; }
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

        [JsonPropertyName("enemyRespawn")]
        public string? EnemyRespawn { get; set; }
    }

    private sealed class DefEntity
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = EntityConstants.TypeEnemy;

        [JsonPropertyName("id")]
        public string Id { get; set; } = "";

        [JsonPropertyName("roomId")]
        public byte RoomId { get; set; }

        [JsonPropertyName("position")]
        public DefPosition Position { get; set; } = new();

        [JsonPropertyName("properties")]
        public DefEntityProperties Properties { get; set; } = new();
    }

    private sealed class DefPosition
    {
        [JsonPropertyName("x")]
        public int X { get; set; }

        [JsonPropertyName("y")]
        public int Y { get; set; }
    }

    private sealed class DefEntityProperties
    {
        [JsonPropertyName("kind")]
        public string Kind { get; set; } = "";

        [JsonPropertyName("palette")]
        public string? Palette { get; set; }

        [JsonPropertyName("facing")]
        public string? Facing { get; set; }

        [JsonPropertyName("respawn")]
        public string? Respawn { get; set; }

        [JsonPropertyName("despawnOffscreen")]
        public bool? DespawnOffscreen { get; set; }
    }
}
