namespace StageSmith.Core.Models;

/// <summary>
/// プレイヤー開始位置。.def の stage.start に対応する。
/// 部屋は Page.Id で参照し、roomId は .def 出力時に Page.Header.RoomId から解決する。
/// </summary>
public sealed class PlayerStart
{
    public Guid PageId { get; set; }

    /// <summary>足元中心の部屋内ピクセルX（0〜255）。</summary>
    public int X { get; set; }

    /// <summary>足元中心の部屋内ピクセルY（0〜239）。</summary>
    public int Y { get; set; }

    public PlayerStart Clone() => new() { PageId = PageId, X = X, Y = Y };
}
