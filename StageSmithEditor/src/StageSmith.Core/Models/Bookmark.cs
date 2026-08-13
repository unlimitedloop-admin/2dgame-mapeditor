namespace StageSmith.Core.Models;

/// <summary>
/// 将来のエディタで使うブックマーク情報（1件）を表すクラス。
/// </summary>
public sealed class Bookmark
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public Guid StageId { get; set; }
    public Guid PageId { get; set; }

    public string Label { get; set; } = "";
    public string Description { get; set; } = "";
}
