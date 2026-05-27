using System.Drawing.Drawing2D;
using Timer = System.Windows.Forms.Timer;

namespace StageSmith.Editor.Tools;

public class SelectionTool : ITool, IDisposable
{
    // ===== 選択状態 =====
    private Point? _selectionStart;
    private Point? _selectionEnd;

    public Rectangle? SelectionRect { get; private set; }

    // ===== 移動状態 =====
    private bool _isMoving = false;
    private bool _isCopyMode = false;
    private Point _moveStart;
    private Point _currentOffset;
    private byte[,]? _moveBuffer;   // 移動中のタイルデータを一時的に保持するバッファ

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

    // =========================
    // ITool 実装
    // =========================
    public void OnMouseDown(int x, int y)
    {
        if (SelectionRect.HasValue && SelectionRect.Value.Contains(x, y))
        {
            _isMoving = true;
            _moveStart = new Point(x, y);
            _currentOffset = Point.Empty;

            // 初期状態を設定してタイマー開始
            _isCopyMode = Control.ModifierKeys.HasFlag(Keys.Control);
            _lastCopyMode = _isCopyMode;
            _keyCheckTimer.Start();

            var rect = SelectionRect.Value;
            _moveBuffer = new byte[rect.Width, rect.Height];

            for (var yy = 0; yy < rect.Height; yy++)
                for (var xx = 0; xx < rect.Width; xx++)
                    _moveBuffer[xx, yy] = _getTile(rect.X + xx, rect.Y + yy);

            return;
        }

        StopMarching();

        var (mapW, mapH) = _getMapSize();
        var cx = Math.Clamp(x, 0, mapW - 1);
        var cy = Math.Clamp(y, 0, mapH - 1);

        _selectionStart = new Point(cx, cy);
        _selectionEnd = _selectionStart;

        UpdateSelectionRect();
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

        if (!_selectionStart.HasValue)
            return;

        var (mapW, mapH) = _getMapSize();
        var cx = Math.Clamp(x, 0, mapW - 1);
        var cy = Math.Clamp(y, 0, mapH - 1);

        _selectionEnd = new Point(cx, cy);
        UpdateSelectionRect();

        SelectionChanged?.Invoke();
    }

    public void OnMouseUp(int x, int y)
    {
        var (mapW, mapH) = _getMapSize();

        if (_isMoving && SelectionRect.HasValue)
        {
            if (_currentOffset == Point.Empty)
            {
                // 選択範囲をクリア
                _isMoving = false;
                _moveBuffer = null;
                _keyCheckTimer.Stop();
                ClearSelection();
                return;
            }

            var src = SelectionRect.Value;
            var dst = new Rectangle(
                src.X + _currentOffset.X,
                src.Y + _currentOffset.Y,
                src.Width,
                src.Height
            );

            // 移動先がマップ外に完全に出ていたらキャンセル
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

            SelectionRect = dst;       // 自分で移動先に更新
            _isMoving = false;
            _currentOffset = Point.Empty;
            _moveBuffer = null;
            _keyCheckTimer.Stop();     // キーチェック停止
            _marchTimer.Start();       // 移動確定後も継続
            SelectionChanged?.Invoke();
            return;
        }

        if (!_selectionStart.HasValue) return;

        var cx = Math.Clamp(x, 0, mapW - 1);
        var cy = Math.Clamp(y, 0, mapH - 1);

        _selectionEnd = new Point(cx, cy);
        UpdateSelectionRect();

        _selectionStart = null;
        _selectionEnd = null;

        _marchTimer.Start();           // 選択確定で常に開始
        SelectionChanged?.Invoke();
    }

    public Cursor GetCursor(int x, int y)
    {
        if (_isMoving)
            return Cursors.SizeAll;

        if (SelectionRect.HasValue && SelectionRect.Value.Contains(x, y))
            return Cursors.SizeAll;

        return Cursors.Default;
    }

    // =========================
    // 外部操作
    // =========================

    /// <summary>
    /// 選択範囲の確定操作（Enter キーなど）を通知する。
    /// 選択範囲がある場合に Confirmed イベントを発火し、呼び出し元が処理を行う。
    /// 選択範囲がなければ何もしない。
    /// </summary>
    public void OnConfirm()
    {
        if (SelectionRect == null) return;
        Confirmed?.Invoke();
        ClearSelection();
    }

    /// <summary>
    /// OnConfirm() が呼ばれたとき（選択範囲がある場合のみ）に発火する。
    /// 塗りつぶしやその他の確定処理はこのイベントを購読して実装する。
    /// </summary>
    public event Action? Confirmed;

    public void ClearSelection()
    {
        SelectionRect = null;
        _selectionStart = null;
        _selectionEnd = null;
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
    private void UpdateSelectionRect()
    {
        if (!_selectionStart.HasValue)
        {
            SelectionRect = null;
            return;
        }

        var start = _selectionStart.Value;
        var end = _selectionEnd ?? start;

        var left = Math.Min(start.X, end.X);
        var top = Math.Min(start.Y, end.Y);
        var right = Math.Max(start.X, end.X);
        var bottom = Math.Max(start.Y, end.Y);

        SelectionRect = new Rectangle(left, top, right - left + 1, bottom - top + 1);
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
        var rect = SelectionRect;

        if (rect == null)
            yield break;

        for (var y = rect.Value.Top; y < rect.Value.Bottom; y++)
        {
            for (var x = rect.Value.Left; x < rect.Value.Right; x++)
            {
                yield return (x, y);
            }
        }
    }

    public void DrawOverlay(Graphics g, int tileSize)
    {
        if (SelectionRect is not Rectangle rect) return;

        var pxRect = new Rectangle(
            rect.X * tileSize,
            rect.Y * tileSize,
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

    public void DrawMovingOverlay(Graphics g, int tileSize, Bitmap? tileset)
    {
        if (!_isMoving || SelectionRect == null || _moveBuffer == null || tileset == null)
            return;

        var rect = SelectionRect.Value;
        var tilesPerRow = tileset.Width / tileSize;

        using var attr = new System.Drawing.Imaging.ImageAttributes();
        var matrix = new System.Drawing.Imaging.ColorMatrix { Matrix33 = 0.5f };
        attr.SetColorMatrix(matrix);

        for (var y = 0; y < rect.Height; y++)
        {
            for (var x = 0; x < rect.Width; x++)
            {
                var id = _moveBuffer[x, y];

                var sx = (id % tilesPerRow) * tileSize;
                var sy = (id / tilesPerRow) * tileSize;

                var dst = new Rectangle(
                    (rect.X + _currentOffset.X + x) * tileSize,
                    (rect.Y + _currentOffset.Y + y) * tileSize,
                    tileSize, tileSize);

                g.DrawImage(tileset, dst, sx, sy, tileSize, tileSize,
                    GraphicsUnit.Pixel, attr);
            }
        }
    }

    public void Dispose()
    {
        _marchTimer.Dispose();
        _keyCheckTimer.Dispose();
        GC.SuppressFinalize(this);
    }
}
