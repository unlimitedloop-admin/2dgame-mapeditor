namespace StageSmith.Core.Models;

/// <summary>
/// .def の entities[].properties に対応する値。
/// EntityTemplate（配置時の既定値）と EntityPlacement（実際の配置値）の両方で使う。
/// null の任意項目は .def に出力せず、ゲーム側の既定値に任せる。
/// </summary>
public sealed class EntityProperties
{
    /// <summary>敵定義の id（例: "metall"）。必須。</summary>
    public string Kind { get; set; } = string.Empty;

    /// <summary>パレットプリセット id。null ならゲーム側既定の "default"。</summary>
    public string? Palette { get; set; }

    /// <summary>EntityConstants.FacingValues のいずれか。null ならゲーム側既定の "player"。</summary>
    public string? Facing { get; set; }

    /// <summary>EntityConstants.RespawnValues のいずれか。null なら部屋（Page.EnemyRespawn）の既定に従う。</summary>
    public string? Respawn { get; set; }

    /// <summary>null ならゲーム側既定の true。</summary>
    public bool? DespawnOffscreen { get; set; }

    public void Normalize()
    {
        Kind ??= string.Empty;

        // 空文字は「未指定」と同じ扱いにそろえる（UIのコンボで空を選んだ場合など）
        if (string.IsNullOrWhiteSpace(Palette)) Palette = null;
        if (string.IsNullOrWhiteSpace(Facing)) Facing = null;
        if (string.IsNullOrWhiteSpace(Respawn)) Respawn = null;
    }

    public EntityProperties Clone()
    {
        return new EntityProperties
        {
            Kind             = Kind,
            Palette          = Palette,
            Facing           = Facing,
            Respawn          = Respawn,
            DespawnOffscreen = DespawnOffscreen,
        };
    }
}
