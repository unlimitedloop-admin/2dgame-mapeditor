using StageSmith.Core.Constants;
using StageSmith.Core.Models;
using StageSmith.Editor.Utilities;
using System.ComponentModel;

namespace StageSmith.Editor.Controls;

// このクラスは partial class として以下のファイルに分割されている（責務ごと）。
// 分割方針は MapViewControl.cs と同じ（CS-001 3.3節参照）。
//   - NodeEditView.cs          : 状態・イベント・公開API・コンストラクタ・Dispose（本体）
//   - NodeEditView.Rendering.cs: OnPaint と Draw* 系メソッド
//   - NodeEditView.Geometry.cs : 座標変換・ヒットテスト（描画/入力の両方から使われる共通部分）
//   - NodeEditView.Input.cs    : マウス操作
//
// NOTE: プレビューキャッシュ（旧 _previewCache 関連ロジック）は、このクラスの
// UI状態（ズーム・選択・マウス操作）に一切依存しない独立した関心事だったため、
// partial class ではなく StageSmith.Editor.Utilities.NodePreviewCache という
// 独立クラスへ抽出した。
//
// MIGRATION NOTE: 名前空間を StageSmith.Editor から StageSmith.Editor.Controls へ修正。
// 物理的に Controls ディレクトリ配下にありながら、CS-001 3.2節の
// 「ディレクトリ名＝名前空間」規約に反して StageSmith.Editor のままになっていた
// 既存の不一致を解消した。このクラスを参照している他ファイル（PageNodeEditorForm等）
// 側で using StageSmith.Editor.Controls; の追加が必要になる場合がある。

/// <summary>
/// ノードエディタの中央描画領域。
/// OnPaintによるカスタム描画でノードを表示する。
/// </summary>
public partial class NodeEditView : Panel
{
    //========================
    // データ参照
    //========================
    private readonly EditorContext _context;

    //========================
    // イベント
    //========================
    /// <summary>ノードがクリックで選択されたとき発火する。</summary>
    public event Action<int>? PageSelected;

    /// <summary>ホバー座標が変わったとき発火する（ステータスバー更新用）。</summary>
    public event Action<int, int>? CoordChanged;

    /// <summary>ノードがダブルクリックされたとき発火する（編集ダイアログ用）。</summary>
    public event Action<int>? PageDoubleClick;

    /// <summary>
    /// ビューオフセットが変化したとき発火する。
    /// PageNodeEditorFormがスクロールバーの位置同期に使用する。
    /// </summary>
    public event Action<PointF>? ViewOffsetChanged;

    /// <summary>
    /// ズーム倍率が変化したとき発火する。
    /// PageNodeEditorFormがステータスバーの倍率表示更新に使用する。
    /// </summary>
    public event Action<float>? ZoomChanged;

    //========================
    // ビュー状態
    //========================
    /// <summary>ビューのオフセット（ドラッグ移動量）。</summary>
    private PointF _viewOffset = PointF.Empty;

    /// <summary>ズーム倍率。</summary>
    private float _zoom = 1.0f;
    private const float ZoomMin  = 0.25f;
    private const float ZoomMax  = 4.0f;
    private const float ZoomStep = 0.25f;

    //========================
    // ノードサイズ（基準 1.0x）
    //========================
    private const int NodeW   = 32;
    private const int NodeH   = 30;
    private const int NodeGap = 4;

    //========================
    // 選択・ホバー状態
    //========================
    public int SelectedPageIndex { get; private set; } = -1;
    private Point _hoveredGridPos = new(-1, -1);

    //========================
    // ドラッグ判定
    //========================
    private Point _mouseDownPos;
    private bool  _isDragging;
    private const int DragThreshold = 4;

    //========================
    // ノードD&D用
    //========================
    private int   _dragSourcePageIndex = -1;    // ドラッグ中のページインデックス
    private bool  _isDraggingNode      = false; // ノードD&D中か
    private bool  _isDraggingNodeCopy  = false; // 複製D&D中か（Ctrl+Shift）
    private Point? _dragCurrentGridPos = null;  // 現在のグリッド座標（ゴースト描画用）

    /// <summary>ノード移動が確定したとき発火する。PageNodeEditorFormがStageManagerを更新する。</summary>
    public event Action<int, int, int>? NodeMoved;   // (pageIndex, newX, newY)

    /// <summary>ノード複製が確定したとき発火する。</summary>
    public event Action<int, int, int>? NodeCopied;  // (pageIndex, newX, newY)

    //========================
    // 複製モード（コンテキストメニュー「このページを複製」）
    //========================
    private bool _isPasteMode         = false;  // 複製モード中か
    private int _pasteModeSourceIndex = -1;     // 複製元ページインデックス
    private Point? _pasteHoverGridPos = null;   // ホバー中のグリッド座標

    /// <summary>複製モードで貼り付け先が確定したとき発火する。</summary>
    /// <remarks>targetPageIndex が -1 なら候補位置への新規追加、0以上なら既存ページへの上書き。</remarks>
    public event Action<int, int, int, int>? PasteModeConfirmed; // (sourcePageIndex, targetPageIndex, newX, newY)

    /// <summary>複製モードを開始する。</summary>
    public void StartPasteMode(int sourcePageIndex)
    {
        _isPasteMode          = true;
        _pasteModeSourceIndex = sourcePageIndex;
        _pasteHoverGridPos    = null;
        Cursor                = Cursors.Cross;
        Invalidate();
    }

    /// <summary>複製モードを終了する。</summary>
    public void CancelPasteMode()
    {
        _isPasteMode          = false;
        _pasteModeSourceIndex = -1;
        _pasteHoverGridPos    = null;
        Cursor                = Cursors.Default;
        Invalidate();
    }

    /// <summary>複製モード中か。</summary>
    public bool IsPasteMode => _isPasteMode;

    //========================
    // 右クリック時のヒットテスト結果保持
    //========================
    private int    _contextMenuTargetPageIndex = -1;
    private Point? _contextMenuTargetCandidate = null;

    //========================
    // Z座標フィルタ（PageNodeEditorFormから設定される）
    //========================
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int FilterZ { get; set; } = 0;

    private NumberDisplayFormat _numberDisplayFormat;

    public void SetNumberDisplayFormat(NumberDisplayFormat format)
    {
        _numberDisplayFormat = format;
        Invalidate();
    }

    /// <summary>
    /// falseの場合、ズーム倍率に関わらずタイルプレビューを描画しない（常に色+RoomIDテキスト表示）。
    /// </summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool ShowPreview { get; set; } = true;

    public void SetShowPreview(bool show)
    {
        ShowPreview = show;
        Invalidate();
    }

    //========================
    // タイルプレビューキャッシュ
    //========================
    private readonly SafeTilesetHolder _tilesetHolder = new();

    // プレビュー表示切り替えの閾値
    private const float PreviewZoomThreshold = 1.5f;

    // プレビュー生成サイズ（1.5x時のノードサイズに合わせる）
    private const int PreviewW = (int)(NodeW * PreviewZoomThreshold);
    private const int PreviewH = (int)(NodeH * PreviewZoomThreshold);

    private readonly NodePreviewCache _previewCache = new(PreviewW, PreviewH);

    /// <summary>
    /// タイルセット画像を設定し、全ページのプレビューを一括生成する。
    /// プロジェクト読み込み時・タイルセット変更時に呼び出す。
    /// </summary>
    public void SetTileset(Bitmap? tileset, IReadOnlyList<Page>? pages)
    {
        _tilesetHolder.Replace(tileset);
        _previewCache.Rebuild(pages, tileset);
    }

    /// <summary>
    /// 指定ページのプレビューキャッシュを再生成する。
    /// ページ編集後に呼び出す。
    /// </summary>
    public void InvalidatePageCache(Guid pageId, Page page)
    {
        _previewCache.InvalidatePage(pageId, page, _tilesetHolder.Current);
        Invalidate();
    }

    //========================
    // 初期化
    //========================
    public NodeEditView(EditorContext context)
    {
        _context = context;

        DoubleBuffered = true;
        BackColor      = Color.FromArgb(40, 40, 45);

        MouseDown   += OnMouseDown;
        MouseMove   += OnMouseMove;
        MouseUp     += OnMouseUp;
        MouseWheel  += OnMouseWheel;
        DoubleClick += OnDoubleClick;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _previewCache.Dispose();
            _tilesetHolder.Dispose();
        }
        base.Dispose(disposing);
    }

    //========================
    // 公開メソッド
    //========================

    /// <summary>
    /// ビューオフセットを外部から設定する（スクロールバー操作時に使用）。
    /// </summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public PointF ViewOffset
    {
        get => _viewOffset;
        set
        {
            _viewOffset = value;
            Invalidate();
        }
    }

    /// <summary>
    /// ノードをパネル中央基準で表示する初期配置を行う。
    /// ウィンドウ表示後に呼び出すこと。
    /// </summary>
    public void CenterView()
    {
        _viewOffset = new PointF(
            Width  / 2f - NodeW * _zoom / 2f,
            Height / 2f - NodeH * _zoom / 2f
        );
        ViewOffsetChanged?.Invoke(_viewOffset);
        Invalidate();
    }

    /// <summary>
    /// MainFormからページ切り替えが通知されたとき、選択状態を同期する。
    /// </summary>
    public void SetSelectedPage(int pageIndex)
    {
        SelectedPageIndex = pageIndex;
        Invalidate();
    }

    /// <summary>
    /// 右クリック時のヒットテスト。設定済みページに当たればインデックスを返す。
    /// コンテキストメニューの状態切り替えに使用する。
    /// </summary>
    public int? HitTestPage()
    {
        return _contextMenuTargetPageIndex >= 0
            ? _contextMenuTargetPageIndex
            : null;
    }

    /// <summary>
    /// 右クリック時のヒットテスト。候補位置に当たればグリッド座標を返す。
    /// コンテキストメニューの「部屋の割り当て」表示制御に使用する。
    /// </summary>
    public Point? HitTestCandidateResult() => _contextMenuTargetCandidate;
}
