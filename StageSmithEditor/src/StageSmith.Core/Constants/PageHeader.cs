namespace StageSmith.Core.Constants;

public enum ScrollType : byte
{
    None = 0, // 隣接なし
    Free = 1, // 任意スクロール
    Page = 2, // ページ単位
    Locked = 3, // スクロール不可
    Axis = 4, // 軸補正
    Auto = 5, // オート
    Object = 6, // オブジェクト依存
    Dynamic = 7  // イベント型
}

[Flags]
public enum ScrollFlags : byte
{
    None = 0,
    NoEdge = 1 << 0, // 淵なし
    Loop = 1 << 1, // ループ
    // まだ余裕あり
}

[Flags]
public enum PageFlags : byte
{
    None = 0,

    //========================
    // 環境系
    //========================
    IsWater = 1 << 0, // 水中
    HasDamageFloor = 1 << 1, // ダメージ床

    //========================
    // 表示系
    //========================
    HasBgAnimation = 1 << 2, // BGアニメあり

    //========================
    // イベント系
    //========================
    HasEvent = 1 << 3, // イベントあり（def参照）

    //========================
    // 特殊状態
    //========================
    DisablePlayerControl = 1 << 4, // 入場時操作不可

    //========================
    // 予約
    //========================
    Reserved1 = 1 << 5,
    Reserved2 = 1 << 6,
    Reserved3 = 1 << 7
}

public static class ScrollEncoding
{
    // 上位4bit = Type
    // 下位4bit = Flags
    public static byte Encode(ScrollType type, ScrollFlags flags)
    {
        return (byte)(((byte)type << 4) | ((byte)flags & 0x0F));
    }

    public static ScrollType GetType(byte value)
    {
        return (ScrollType)(value >> 4);
    }

    public static ScrollFlags GetFlags(byte value)
    {
        return (ScrollFlags)(value & 0x0F);
    }
}

public struct PageHeader
{
    //========================
    // 識別
    //========================
    public byte MagicStart; // 0xA5

    //========================
    // フラグ
    //========================
    public PageFlags Flags;

    //========================
    // 隣接ページ
    //========================
    public byte LeftPage;
    public byte RightPage;
    public byte UpPage;
    public byte DownPage;

    //========================
    // スクロール（1byte×4方向）
    //========================
    public byte ScrollLeft;
    public byte ScrollRight;
    public byte ScrollUp;
    public byte ScrollDown;

    //========================
    // Z座標
    //========================
    public byte Z;

    //========================
    // 予約領域（将来用）
    //========================
    public byte Reserved1;
    public byte Reserved2;
    public byte Reserved3;

    //========================
    // 終端
    //========================
    public byte MagicEnd; // 0x5A

    //========================
    // バイト化
    //========================
    public byte[] ToBytes()
    {
        return new byte[]
        {
            MagicStart,
            (byte)Flags,

            LeftPage,
            RightPage,
            UpPage,
            DownPage,

            ScrollLeft,
            ScrollRight,
            ScrollUp,
            ScrollDown,

            Z,

            Reserved1,
            Reserved2,
            Reserved3,

            MagicEnd
        };
    }
}
