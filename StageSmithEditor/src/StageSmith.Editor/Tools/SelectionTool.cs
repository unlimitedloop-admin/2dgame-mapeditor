using StageSmith.Core.Constants;
using System.Drawing.Drawing2D;
using Timer = System.Windows.Forms.Timer;

namespace StageSmith.Editor.Tools;

/// <summary>
/// 選択ツール。矩形選択、複数選択、移動/コピー、Shift+クリック拡張などをサポートする。
/// </summary>
public class SelectionTool : ITool, IDisposable
{
    // ===== 選択状態 =====
    private Point? _dragStart;
    private Point? _dragEnd;
    private Point? _anchorPoint;        // Shift+クリック拡張の基準点（＝最初にドラッグを始めた角）
    private Rectangle? _draggingRect;   // ドラッグ中・未確定のプレビュー矩形

    private readonly List<Rectangle> _selectionRects = [];

    /// <summary>確定済みの選択矩形一覧。</summary>
    public IReadOnlyList<Rectangle> SelectionRects => _selectionRects;

    /// <summary>
    /// 後方互換用：単一選択時のみ値を返す。
    /// 複数選択中（2個以上）は null になる。
    /// </summary>
    public Rectangle? SelectionRect => _selectionRects.Count == 1 ? _selectionRects[0] : null;

    // ===== 移動状態 =====
    private bool _isMoving = false;
    private bool _isCopyMode = false;
    private Point _moveStart;
    private Point _currentOffset;
    private byte[,]? _moveBuffer;   // 移動中のタイルデータを一時的に保持するバッファ

    /// <summary>
    /// 選択矩形の移動が要求されたときに発生するイベント。
    /// 引数は移動対象の矩形、移動量のオフセット、コピーかどうかを示すフラグ。
    /// </summary>
    public event Action<Rectangle, Point, bool>? MoveRequested;

    // ===== マーチングアント =====
    private readonly Timer _marchTimer = new() { Interval = 80 };
    private float _dashOffset = 0f;

    public float DashOffset => _dashOffset;

    // ===== キー状態チェック =====
    private readonly Timer _keyCheckTimer = new() { Interval = 50 };
    private bool _lastCopyMode = false;

    // ===== 通知 =====
    public event Action? SelectionChanged;

    // ===== デリゲート =====
    private readonly Func<int, int, byte> _getTile;
    private readonly Func<(int w, int h)> _getMapSize;

    public SelectionTool(Func<int, int, byte> getTile, Func<(int w, int h)> getMapSize)
    {
        _getTile = getTile;
        _getMapSize = getMapSize;
        _marchTimer.Tick += (_, _) =>
        {
            _dashOffset = (_dashOffset + 1f) % 24f;
            SelectionChanged?.Invoke();
        };

        _keyCheckTimer.Tick += (_, _) =>
        {
            if (!_isMoving) return;

            var currentCopyMode = Control.ModifierKeys.HasFlag(Keys.Control);
            if (currentCopyMode != _lastCopyMode)
            {
                _isCopyMode = currentCopyMode;
                _lastCopyMode = currentCopyMode;
                SelectionChanged?.Invoke();
            }
        };
    }

    /// <summary>
    /// 複数の確定済み矩形を外部からまとめて設定する。
    /// メニューコマンド（Select All Same Tile / Select Empty Tile など）から使用する。
    /// ドラッグ中の状態や単一選択の拡張基準点はリセットする。
    /// </summary>
    public void SetSelectionRects(IEnumerable<Rectangle> rects)
    {
        _selectionRects.Clear();
        _selectionRects.AddRange(rects);

        _dragStart = null;
        _dragEnd = null;
        _draggingRect = null;
        _anchorPoint = null;   // 複数選択なのでShift拡張の基準点は持たせない
        _isMoving = false;
        _moveBuffer = null;
        _currentOffset = Point.Empty;
        _keyCheckTimer.Stop();

        StopMarching();
        if (_selectionRects.Count > 0)
            _marchTimer.Start();

        SelectionChanged?.Invoke();
    }

    // =========================
    // ITool 実装
    // =========================
    public void OnMouseDown(int x, int y)
    {
        var isCtrl = Control.ModifierKeys.HasFlag(Keys.Control);

        // 単一選択の「内側」：Ctrlの有無に関わらず移動/コピー開始（従来通り）
        if (_selectionRects.Count == 1 && _selectionRects[0].Contains(x, y))
        {
            BeginMove(x, y);
            return;
        }

        if (isCtrl)
        {
            // Ctrl+外側ドラッグ：既存の選択を残したまま新しい矩形を追加する
            // （既存矩形と被っても気にせず継続する）
            BeginNewRectDrag(x, y);
            return;
        }

        // Ctrlなしの外側クリック：既存の選択（複数含む）を全てクリアして新規選択を開始する
        StopMarching();
        _selectionRects.Clear();
        BeginNewRectDrag(x, y);
    }

    private void BeginMove(int x, int y)
    {
        _isMoving = true;
        _moveStart = new Point(x, y);
        _currentOffset = Point.Empty;

        _isCopyMode = Control.ModifierKeys.HasFlag(Keys.Control);
        _lastCopyMode = _isCopyMode;
        _keyCheckTimer.Start();

        var rect = _selectionRects[0];
        _moveBuffer = new byte[rect.Width, rect.Height];

        for (var yy = 0; yy < rect.Height; yy++)
            for (var xx = 0; xx < rect.Width; xx++)
                _moveBuffer[xx, yy] = _getTile(rect.X + xx, rect.Y + yy);
    }

    private void BeginNewRectDrag(int x, int y)
    {
        var (mapW, mapH) = _getMapSize();
        var cx = Math.Clamp(x, 0, mapW - 1);
        var cy = Math.Clamp(y, 0, mapH - 1);

        _dragStart = new Point(cx, cy);
        _dragEnd = _dragStart;
        _anchorPoint = _dragStart;   // Shift+クリック拡張用の基準点
        UpdateDraggingRect();
        SelectionChanged?.Invoke();
    }

    public void OnMouseMove(int x, int y)
    {
        if (_isMoving)
        {
            _currentOffset = new Point(x - _moveStart.X, y - _moveStart.Y);
            SelectionChanged?.Invoke();
            return;
        }

        if (!_dragStart.HasValue)
            return;

        var (mapW, mapH) = _getMapSize();
        var cx = Math.Clamp(x, 0, mapW - 1);
        var cy = Math.Clamp(y, 0, mapH - 1);

        _dragEnd = new Point(cx, cy);
        UpdateDraggingRect();

        SelectionChanged?.Invoke();
    }

    public void OnMouseUp(int x, int y)
    {
        var (mapW, mapH) = _getMapSize();

        if (_isMoving && _selectionRects.Count == 1)
        {
            if (_currentOffset == Point.Empty)
            {
                _isMoving = false;
                _moveBuffer = null;
                _keyCheckTimer.Stop();
                ClearSelection();
                return;
            }

            var src = _selectionRects[0];
            var dst = new Rectangle(
                src.X + _currentOffset.X,
                src.Y + _currentOffset.Y,
                src.Width,
                src.Height
            );

            var mapRect = new Rectangle(0, 0, mapW, mapH);
            if (!dst.IntersectsWith(mapRect))
            {
                _isMoving = false;
                _currentOffset = Point.Empty;
                _moveBuffer = null;
                _keyCheckTimer.Stop();
                _marchTimer.Start();
                SelectionChanged?.Invoke();
                return;
            }

            MoveRequested?.Invoke(src, _currentOffset, _isCopyMode);

            if (_anchorPoint.HasValue)
            {
                _anchorPoint = new Point(
                    _anchorPoint.Value.X + _currentOffset.X,
                    _anchorPoint.Value.Y + _currentOffset.Y);
            }

            _selectionRects[0] = dst;
            _isMoving = false;
            _currentOffset = Point.Empty;
            _moveBuffer = null;
            _keyCheckTimer.Stop();
            _marchTimer.Start();
            SelectionChanged?.Invoke();
            return;
        }

        if (!_dragStart.HasValue) return;

        var cx = Math.Clamp(x, 0, mapW - 1);
        var cy = Math.Clamp(y, 0, mapH - 1);

        _dragEnd = new Point(cx, cy);
        UpdateDraggingRect();

        if (_draggingRect.HasValue)
            _selectionRects.Add(_draggingRect.Value);

        _dragStart = null;
        _dragEnd = null;
        _draggingRect = null;

        _marchTimer.Start();
        SelectionChanged?.Invoke();
    }

    public Cursor GetCursor(int x, int y)
    {
        if (_isMoving)
            return Cursors.SizeAll;

        if (_selectionRects.Count == 1 && _selectionRects[0].Contains(x, y))
            return Cursors.SizeAll;

        return Cursors.Default;
    }

    // =========================
    // 外部操作
    // =========================
    /// <summary>
    /// OnConfirm() が呼ばれたとき（選択範囲がある場合のみ）に発火する。
    /// 塗りつぶしやその他の確定処理はこのイベントを購読して実装する。
    /// </summary>
    public event Action? Confirmed;

    public void OnConfirm()
    {
        if (_selectionRects.Count == 0) return;
        Confirmed?.Invoke();
        ClearSelection();
    }

    public void ClearSelection()
    {
        _selectionRects.Clear();
        _anchorPoint = null;
        _dragStart = null;
        _dragEnd = null;
        _draggingRect = null;
        _isMoving = false;
        _moveBuffer = null;
        _currentOffset = Point.Empty;

        _keyCheckTimer.Stop();
        StopMarching();
        SelectionChanged?.Invoke();
    }

    public bool IsCopyModeActive() => _isMoving && _isCopyMode;

    // =========================
    // 内部処理
    // =========================
    private void UpdateDraggingRect()
    {
        if (!_dragStart.HasValue)
        {
            _draggingRect = null;
            return;
        }

        var start = _dragStart.Value;
        var end = _dragEnd ?? start;

        var left = Math.Min(start.X, end.X);
        var top = Math.Min(start.Y, end.Y);
        var right = Math.Max(start.X, end.X);
        var bottom = Math.Max(start.Y, end.Y);

        _draggingRect = new Rectangle(left, top, right - left + 1, bottom - top + 1);
    }

    private void StopMarching()
    {
        _marchTimer.Stop();
        _dashOffset = 0f;
    }

    // =========================
    // 描画補助（オプション）
    // =========================
    public IEnumerable<(int x, int y)> GetSelectedPositions()
    {
        var seen = new HashSet<(int x, int y)>();

        foreach (var rect in _selectionRects)
        {
            for (var y = rect.Top; y < rect.Bottom; y++)
            {
                for (var x = rect.Left; x < rect.Right; x++)
                {
                    if (seen.Add((x, y)))
                        yield return (x, y);
                }
            }
        }
    }

    public void DrawOverlay(Graphics g, int tileSize, int marginX, int marginY)
    {
        foreach (var rect in _selectionRects)
            DrawRectOverlay(g, rect, tileSize, marginX, marginY);

        if (_draggingRect is { } dragging)
            DrawRectOverlay(g, dragging, tileSize, marginX, marginY);
    }

    public void DrawMovingOverlay(Graphics g, int tileSize, Bitmap? tileset, int marginX, int marginY)
    {
        if (!_isMoving || _selectionRects.Count == 1 || _moveBuffer == null || tileset == null)
            return;

        var rect = _selectionRects[0];

        // srcSize: タイル画像の論理サイズ（切り出し用 = 16px）
        // tileSize: 画面上の描画サイズ（表示用 = 32px）
        var srcSize = MapConstants.DefaultTileSize;
        var tilesPerRow = tileset.Width / srcSize;

        using var attr = new System.Drawing.Imaging.ImageAttributes();
        var matrix = new System.Drawing.Imaging.ColorMatrix { Matrix33 = 0.5f };
        attr.SetColorMatrix(matrix);

        for (var y = 0; y < rect.Height; y++)
        {
            for (var x = 0; x < rect.Width; x++)
            {
                var id = _moveBuffer[x, y];

                // 切り出し位置は srcSize（16px）基準
                var sx = (id % tilesPerRow) * srcSize;
                var sy = (id / tilesPerRow) * srcSize;

                // 描画位置は tileSize（32px）基準で拡大表示、マージン考慮
                var dst = new Rectangle(
                    marginX + (rect.X + _currentOffset.X + x) * tileSize,
                    marginY + (rect.Y + _currentOffset.Y + y) * tileSize,
                    tileSize, tileSize);

                g.DrawImage(tileset, dst, sx, sy, srcSize, srcSize,
                    GraphicsUnit.Pixel, attr);
            }
        }
    }

    private void DrawRectOverlay(Graphics g, Rectangle rect, int tileSize, int marginX, int marginY)
    {
        var pxRect = new Rectangle(
            marginX + rect.X * tileSize,
            marginY + rect.Y * tileSize,
            rect.Width * tileSize,
            rect.Height * tileSize
        );

        using var bgPen = new Pen(Color.Black, 2f);
        g.DrawRectangle(bgPen, pxRect);

        using var pen = new Pen(Color.LimeGreen, 2f);
        pen.DashStyle = DashStyle.Custom;
        pen.DashPattern = [4f, 4f];
        pen.DashOffset = _dashOffset;
        g.DrawRectangle(pen, pxRect);
    }

    /// <summary>
    /// Shift+クリックによる選択範囲の対角拡張。
    /// 単一選択（矩形が1個）の場合のみ有効。0個・2個以上のときは何もしない。
    /// </summary>
    public void ExtendSelection(int x, int y)
    {
        if (_selectionRects.Count != 1 || !_anchorPoint.HasValue)
            return;

        var (mapW, mapH) = _getMapSize();
        var cx = Math.Clamp(x, 0, mapW - 1);
        var cy = Math.Clamp(y, 0, mapH - 1);

        var anchor = _anchorPoint.Value;

        var left = Math.Min(anchor.X, cx);
        var top = Math.Min(anchor.Y, cy);
        var right = Math.Max(anchor.X, cx);
        var bottom = Math.Max(anchor.Y, cy);

        _selectionRects[0] = new Rectangle(left, top, right - left + 1, bottom - top + 1);

        SelectionChanged?.Invoke();
    }

    /// <summary>
    /// ページ全体を単一の選択範囲として設定する（Ctrl+A）。
    /// </summary>
    public void SelectAll(int width, int height)
    {
        _selectionRects.Clear();
        _dragStart = null;
        _dragEnd = null;
        _draggingRect = null;
        _isMoving = false;
        _moveBuffer = null;
        _currentOffset = Point.Empty;
        _keyCheckTimer.Stop();

        var rect = new Rectangle(0, 0, width, height);
        _selectionRects.Add(rect);
        _anchorPoint = new Point(0, 0);   // 後述：Shift+クリック拡張用の基準点

        StopMarching();
        _marchTimer.Start();
        SelectionChanged?.Invoke();
    }

    public void Dispose()
    {
        _marchTimer.Dispose();
        _keyCheckTimer.Dispose();
        GC.SuppressFinalize(this);
    }
}
