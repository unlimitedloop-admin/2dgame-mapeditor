namespace StageSmith.Core.Models;

public sealed class Bookmark
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public Guid StageId { get; set; }
    public Guid PageId { get; set; }

    public string Label { get; set; } = "";
    public string Description { get; set; } = "";
}
