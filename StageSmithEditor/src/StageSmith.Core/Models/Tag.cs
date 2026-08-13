namespace StageSmith.Core.Models;

/// <summary>
/// プロジェクト全体で共有するタグ定義。
/// Stage/Pageはこの Id（Guid）を参照する形で付与する。
/// </summary>
public class Tag
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string Label { get; set; } = string.Empty;

    /// <summary>#RRGGBB形式。アイコンの背景/縁取りなど独立要素として使用する（アイコン自体には着色しない）。</summary>
    public string Color { get; set; } = "#FFFFFF";

    public int Priority { get; set; } = 0;

    /// <summary>
    /// タグアイコン画像の相対パス（BaseDirectoryからの相対、例: "TagIcons/todo.png"）。
    /// デフォルト素材・カスタムアップロードの区別なく、常にプロジェクトのTagIcons/配下へコピーしたものを指す。
    /// 未設定時はアイコン無し（色のみで表示）。
    /// </summary>
    public string? IconPath { get; set; }
}
