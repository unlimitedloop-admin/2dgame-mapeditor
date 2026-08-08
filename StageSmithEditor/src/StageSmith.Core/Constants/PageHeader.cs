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

/// <summary>
/// 方向ごとのスクロール種別。$08〜$0Bにそれぞれ1バイト・生値のまま格納する
/// （ニブル分割なし）。C++側 mm2hack::apps::resources::bg::RoomScrollType と
/// 値を完全一致させること。
///
/// NOTE: 旧版にあった Locked/Axis/Dynamic は廃止した。
///   - Locked（隣接部屋はあるがスクロール不可）は None に統合。
///     「部屋はあるが進めない」状態は、ヘッダー上は本来の種別（例: Page）を
///     保持したまま、ゲームロジック側のイベントで実行時に None 相当へ
///     オーバーライドする方式で表現する（中ボス部屋などのギミック）。
///   - Axis は Loop に統合（水平/垂直の区別はどのフィールドに格納するかで決まる）。
///   - Dynamic は EventDriven に統合。
/// </summary>
public enum ScrollType : byte
{
    None = 0,          // スクロール不可／隣接部屋なし
    Free = 1,          // 自由スクロール（8方向スクロールも上下左右をFreeにすることで実現）
    Page = 2,          // ページ単位スクロール（画面端到達で隣室へ）
    Auto = 3,          // オートスクロール（時間駆動）
    ObjectFollow = 4,  // オブジェクト依存スクロール（プレイヤー以外の座標基準）
    EventDriven = 5,   // イベント駆動型（詳細はゲームロジック/.def側に委ねる）
    Loop = 6,          // ループ部屋（水平/垂直はフィールド位置で決まる）
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

            ScrollLeft = (byte)ScrollType.None,
            ScrollRight = (byte)ScrollType.None,
            ScrollUp = (byte)ScrollType.None,
            ScrollDown = (byte)ScrollType.None,

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
