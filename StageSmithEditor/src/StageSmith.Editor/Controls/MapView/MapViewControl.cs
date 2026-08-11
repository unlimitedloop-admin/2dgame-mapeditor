using StageSmith.Core.Constants;
using StageSmith.Core.Models;
using StageSmith.Editor.Tools;
using StageSmith.Editor.Utilities;
using System.ComponentModel;

namespace StageSmith.Editor.Controls;

// このクラスは partial class として以下のファイルに分割されている（責務ごと）。
// namespace は移動前と同じ StageSmith.Editor.Controls のまま維持し、
// 物理的な配置のみ Controls/MapView/ ディレクトリへ変更している。
//   - MapViewControl.cs                    : フィールド・コンストラクタ・公開API・状態管理（本体）
//   - MapViewControl.Rendering.cs          : OnPaint と全 Draw* 系メソッド
//   - MapViewControl.Input.cs              : マウス操作・座標変換・カーソル制御
//   - MapViewControl.Zoom.cs               : ズーム関連
//   - MapViewControl.AdjacentNavigation.cs : 隣接ページナビゲーションボタン

/// <summary>
/// マップビューを表示するコントロールです。
/// </summary>
public partial class MapViewControl : DoubleBufferedPanel
{
    private TileMap? _tileMap;
    private readonly SafeTilesetHolder _tilesetHolder = new();

    // ModifierKeysの状態をポーリングして、スポイト/バケツのカーソルを更新するためのタイマー
    private readonly System.Windows.Forms.Timer _modifierPollTimer = new() { Interval = 50 };
    private bool _lastIsAlt = false;
    private bool _lastIsShift = false;

    private bool _showGrid = true;
    private bool _showTileInfo = false;

    private readonly ToolTip _tileInfoToolTip = new()
    {
        InitialDelay = 0,
        ReshowDelay = 0,
        AutomaticDelay = 0
    };

    private bool _showTileNumbers = false;

    public void SetShowTileNumbers(bool show)
    {
        _showTileNumbers = show;
        Invalidate();
    }

    private int _currentPageIndex = -1;
    private Guid _currentStageId = Guid.Empty;

    public MapViewControl()
    {
        DoubleBuffered = true;
        ResizeRedraw = true;

        _modifierPollTimer.Tick += (_, _) =>
        {
            var isAlt = (ModifierKeys & Keys.Alt) != 0;
            var isShift = (ModifierKeys & Keys.Shift) != 0;

            if (isAlt != _lastIsAlt || isShift != _lastIsShift)
            {
                _lastIsAlt = isAlt;
                _lastIsShift = isShift;
                UpdateCursor();
            }
        };

        MouseEnter += (_, _) => _modifierPollTimer.Start();
        MouseLeave += (_, _) =>
        {
            _modifierPollTimer.Stop();
            _lastIsAlt = false;
            _lastIsShift = false;
            _tileInfoToolTip.Hide(this);
        };
    }

    // ===== Toggle Grid =====
    private bool _showRowNumbers = false;
    private bool _showColumnNumbers = false;
    private NumberDisplayFormat _numberDisplayFormat = NumberDisplayFormat.Hex;

    public void SetShowRowNumbers(bool show)
    {
        _showRowNumbers = show;
        UpdatePreferredControlSize();
        Invalidate();
    }

    public void SetShowColumnNumbers(bool show)
    {
        _showColumnNumbers = show;
        UpdatePreferredControlSize();
        Invalidate();
    }

    public void SetNumberDisplayFormat(NumberDisplayFormat format)
    {
        _numberDisplayFormat = format;
        Invalidate();
    }

    private int OffsetX =>
        ViewerConstants.MapViewMargin + (_showRowNumbers ? ViewerConstants.RowNumberBandWidth : 0);

    private int OffsetY =>
        ViewerConstants.MapViewMargin + (_showColumnNumbers ? ViewerConstants.ColumnNumberBandHeight : 0);

    private Point _hoverTile = new(-1, -1);

    public Point GetHoverTile() => _hoverTile;

    // ===== Tool =====
    private ToolManager? _toolManager;

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public ToolManager? ToolManager
    {
        get => _toolManager;
        set
        {
            _toolManager?.ToolChanged -= OnToolChanged;
            _toolManager = value;
            _toolManager?.ToolChanged += OnToolChanged;
            Invalidate();
        }
    }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public ITool? PickerTool { get; set; }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public ITool? FillTool { get; set; }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public SelectionTool? SelectionTool { get; set; }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public MarkerTool? MarkerTool { get; set; }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public MarkerState? MarkerState { get; set; }

    private Color _markerColor = Color.Red;

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color MarkerColor => _markerColor;

    public void SetMarkerColor(Color color)
    {
        _markerColor = color;
        Invalidate();
    }

    // ===== Preview =====
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int PreviewTileId { get; set; } = -1;

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public MetaTile? PreviewMetaTile { get; set; }

    /// <summary>
    /// 外部側で特殊ブラシを処理したい場合、MapViewControl内部のTool処理を抑止する。
    /// 例：MetaTile配置中はPenTool/SelectionToolへ入力を渡さない。
    /// </summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Func<bool>? ShouldSuppressToolInput { get; set; }

    // ===== Paste Preview =====
    private bool _showPreview = false;
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool ShowPreview
    {
        get => _showPreview;
        set
        {
            if (_showPreview == value) return;
            _showPreview = value;
            Invalidate();
        }
    }

    // ===== Search Highlight =====
    private IReadOnlyList<TileSearchHit> _searchHighlights = [];
    private TileSearchHit? _currentSearchHit;
    private bool _showSearchHighlight = true;

    /// <summary>
    /// 検索ハイライトの表示状態を更新する。
    /// hits は「現在表示中のページに属するヒットのみ」に絞ってから渡すこと（MainForm側の責務）。
    /// </summary>
    public void SetSearchHighlights(
        IReadOnlyList<TileSearchHit> hits,
        TileSearchHit? currentHit,
        bool showHighlight)
    {
        _searchHighlights = hits;
        _currentSearchHit = currentHit;
        _showSearchHighlight = showHighlight;
        Invalidate();
    }

    public void ClearSearchHighlights()
    {
        SetSearchHighlights([], null, true);
    }

    // =========================
    // セット系
    // =========================
    public void SetTileMap(TileMap? map)
    {
        _tileMap = map;
        UpdatePreferredControlSize();
        Invalidate();
    }

    public void SetTileset(Bitmap? tileset)
    {
        _tilesetHolder.Replace(tileset);
        Invalidate();
    }

    public void SetShowGrid(bool show)
    {
        _showGrid = show;
        Invalidate();
    }

    public void SetShowTileInfo(bool show)
    {
        _showTileInfo = show;
        if (!show) _tileInfoToolTip.Hide(this);
    }

    public void SetCurrentPageIndex(int pageIndex)
    {
        if (_currentPageIndex == pageIndex) return;
        _currentPageIndex = pageIndex;
        Invalidate();
    }

    public void SetCurrentStageId(Guid stageId)
    {
        if (_currentStageId == stageId) return;
        _currentStageId = stageId;
        Invalidate();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _tilesetHolder.Dispose();
            _modifierPollTimer.Dispose();
            _tileInfoToolTip.Dispose();
        }

        base.Dispose(disposing);
    }
}
