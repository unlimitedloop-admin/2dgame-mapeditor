namespace StageSmith.Core.Constants;

[Flags]
public enum PageFlags : byte
{
    None = 0,
    IsWater = 1 << 0,
    HasDamageFloor = 1 << 1,
    HasBgAnimation = 1 << 2,
    HasEvent = 1 << 3,
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

public struct PageHeader
{
    public byte MagicStart { get; set; }
    public PageFlags Flags { get; set; }

    public byte LeftPage { get; set; }
    public byte RightPage { get; set; }
    public byte UpPage { get; set; }
    public byte DownPage { get; set; }

    public byte FrontPage { get; set; }
    public byte BackPage { get; set; }

    public byte ScrollLeft { get; set; }
    public byte ScrollRight { get; set; }
    public byte ScrollUp { get; set; }
    public byte ScrollDown { get; set; }

    public byte Z { get; set; }

    public byte Reserved1 { get; set; }
    public byte Reserved2 { get; set; }

    public byte MagicEnd { get; set; }

    public static PageHeader CreateDefault()
    {
        return new PageHeader
        {
            MagicStart = 0xA5,
            MagicEnd = 0x5A,

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

            Z = 0
        };
    }

    public readonly byte[] ToBytes() => [
            MagicStart,
            (byte)Flags,

            LeftPage,
            RightPage,
            UpPage,
            DownPage,
            FrontPage,
            BackPage,

            ScrollLeft,
            ScrollRight,
            ScrollUp,
            ScrollDown,

            Z,

            Reserved1,
            Reserved2,

            MagicEnd
        ];

    /// <summary>
    /// このヘッダの複製を返す。
    /// struct の値コピーなので全フィールドがそのままコピーされる。
    /// </summary>
    public readonly PageHeader Clone() => this;
}
