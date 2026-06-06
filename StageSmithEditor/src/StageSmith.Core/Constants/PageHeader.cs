namespace StageSmith.Core.Constants;

/// <summary>
/// ページヘッダーのフラグ定義。
/// binファイル $0D に対応する。
/// ゲームアプリ側が参照する演出・状態フラグ。
/// </summary>
[Flags]
public enum PageFlags : byte
{
    None = 0,
    IsWater = 1 << 0,  // bit0: 水中の場面
    IsWind = 1 << 1,  // bit1: 風が吹く場面
    // bit2〜7: 将来拡張用
}

public enum ScrollType : byte
{
    None = 0,
    Free = 1,
    Page = 2,
    Locked = 3,
    Axis = 4,
    Auto = 5,
    Object = 6,
    Dynamic = 7,
}

[Flags]
public enum ScrollFlags : byte
{
    None = 0,
    NoEdge = 1 << 0,
    Loop = 1 << 1,
}

public static class ScrollEncoding
{
    public static byte Encode(ScrollType type, ScrollFlags flags)
        => (byte)(((byte)type << 4) | ((byte)flags & 0x0F));

    public static ScrollType GetType(byte value)
        => (ScrollType)(value >> 4);

    public static ScrollFlags GetFlags(byte value)
        => (ScrollFlags)(value & 0x0F);
}

/// <summary>
/// ページヘッダー構造体。
/// binファイルの $00-$0F（16byte）に対応する。
///
/// アドレスマップ:
///   $00: MagicStart (0xA5)
///   $01: RoomId
///   $02: LeftPage
///   $03: RightPage
///   $04: UpPage
///   $05: DownPage
///   $06: FrontPage
///   $07: BackPage
///   $08: ScrollLeft
///   $09: ScrollRight
///   $0A: ScrollUp
///   $0B: ScrollDown
///   $0C: Z
///   $0D: Flags
///   $0E: Reserved
///   $0F: MagicEnd (0x5A)
/// </summary>
public struct PageHeader
{
    public byte MagicStart { get; set; }  // $00 固定値 0xA5

    public byte RoomId { get; set; }      // $01 自分自身の部屋番号

    public byte LeftPage { get; set; }    // $02
    public byte RightPage { get; set; }   // $03
    public byte UpPage { get; set; }      // $04
    public byte DownPage { get; set; }    // $05
    public byte FrontPage { get; set; }   // $06
    public byte BackPage { get; set; }    // $07

    public byte ScrollLeft { get; set; }  // $08
    public byte ScrollRight { get; set; } // $09
    public byte ScrollUp { get; set; }    // $0A
    public byte ScrollDown { get; set; }  // $0B

    public byte Z { get; set; }           // $0C

    public PageFlags Flags { get; set; }  // $0D

    public byte Reserved { get; set; }    // $0E 予約領域

    public byte MagicEnd { get; set; }    // $0F 固定値 0x5A

    public static PageHeader CreateDefault()
    {
        return new PageHeader
        {
            MagicStart = 0xA5,
            MagicEnd = 0x5A,

            RoomId = 0xFF,
            LeftPage = 0xFF,
            RightPage = 0xFF,
            UpPage = 0xFF,
            DownPage = 0xFF,
            FrontPage = 0xFF,
            BackPage = 0xFF,

            ScrollLeft = ScrollEncoding.Encode(ScrollType.None, ScrollFlags.None),
            ScrollRight = ScrollEncoding.Encode(ScrollType.None, ScrollFlags.None),
            ScrollUp = ScrollEncoding.Encode(ScrollType.None, ScrollFlags.None),
            ScrollDown = ScrollEncoding.Encode(ScrollType.None, ScrollFlags.None),

            Z = 0,
            Flags = PageFlags.None,
        };
    }

    public readonly byte[] ToBytes() =>
    [
        MagicStart,  // $00
        RoomId,      // $01

        LeftPage,    // $02
        RightPage,   // $03
        UpPage,      // $04
        DownPage,    // $05
        FrontPage,   // $06
        BackPage,    // $07

        ScrollLeft,  // $08
        ScrollRight, // $09
        ScrollUp,    // $0A
        ScrollDown,  // $0B

        Z,           // $0C
        (byte)Flags, // $0D
        Reserved,    // $0E
        MagicEnd,    // $0F
    ];

    /// <summary>
    /// このヘッダの複製を返す。
    /// struct の値コピーなので全フィールドがそのままコピーされる。
    /// </summary>
    public readonly PageHeader Clone() => this;
}
