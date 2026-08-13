namespace StageSmith.Core.Models;

/// <summary>
/// マップビュー上の一時的な視覚目印（1件）。
/// ステージ・ページ・座標を持つ。色はEditor層で一括管理する（保存対象外のため）。
/// </summary>
public readonly record struct Marker(Guid StageId, int PageIndex, int X, int Y);
