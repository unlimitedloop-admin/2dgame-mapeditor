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
    private Point _moveStart;
    private Point _currentOffset;
    private byte[,]? _moveBuffer;   // 移動中のタイルデータを一時的に保持するバッファ
    private bool _isCopyMode = false;
    public void SetCopyMode(bool enable) => _isCopyMode = enable;

    public event Action<Rectangle, Point, bool>? MoveRequested;

    // ===== マーチングアント =====
    private readonly Timer _marchTimer = new() { Interval = 80 };
    private float _dashOffset = 0f;

    public float DashOffset => _dashOffset;

    // ===== コピー状態 =====
    private bool _isSelectionCopied = false;
    public bool IsSelectionCopied => _isSelectionCopied;

    // ===== 通知 =====
    public event Action? SelectionChanged;

    // ===== デリゲート =====
    private readonly Func<int, int, byte> _getTile;

    public SelectionTool(Func<int, int, byte> getTile)
    {
        _getTile = getTile;
        _marchTimer.Tick += (_, _) =>
        {
            _dashOffset = (_dashOffset + 1f) % 24f;
            SelectionChanged?.Invoke();
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

            var rect = SelectionRect.Value;
            _moveBuffer = new byte[rect.Width, rect.Height];

            for (var yy = 0; yy < rect.Height; yy++)
            {
                for (var xx = 0; xx < rect.Width; xx++)
                {
                    _moveBuffer[xx, yy] = _getTile(rect.X + xx, rect.Y + yy);
                }
            }
            _isCopyMode = Control.ModifierKeys.HasFlag(Keys.Control);
            return;
        }

        StopMarching();

        _isSelectionCopied = false;

        _selectionStart = new Point(x, y);
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

        _selectionEnd = new Point(x, y);
        UpdateSelectionRect();

        SelectionChanged?.Invoke();
    }

    public void OnMouseUp(int x, int y)
    {
        if (_isMoving && SelectionRect.HasValue)
        {
            var src = SelectionRect.Value;
            var dst = new Rectangle(
                src.X + _currentOffset.X,
                src.Y + _currentOffset.Y,
                src.Width,
                src.Height
            );

            MoveRequested?.Invoke(src, _currentOffset, _isCopyMode);

            SelectionRect = dst;
            _isMoving = false;
            _currentOffset = Point.Empty;
            return;
        }

        if (!_selectionStart.HasValue)
            return;

        _selectionEnd = new Point(x, y);
        UpdateSelectionRect();

        _selectionStart = null;
        _selectionEnd = null;

        SelectionChanged?.Invoke();
    }

    // =========================
    // 外部操作
    // =========================

    public void ClearSelection()
    {
        SelectionRect = null;
        _selectionStart = null;
        _selectionEnd = null;
        _isSelectionCopied = false;
        _isMoving = false;
        _moveBuffer = null;
        _currentOffset = Point.Empty;

        StopMarching();
        SelectionChanged?.Invoke();
    }

    public void SetSelectionCopied(bool copied)
    {
        _isSelectionCopied = copied;

        if (copied)
            _marchTimer.Start();
        else
            StopMarching();

        SelectionChanged?.Invoke();
    }

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

    public void DrawOverlay(Graphics g, int tileSize)
    {
        if (SelectionRect is not Rectangle rect)
            return;

        var pxRect = new Rectangle(
            rect.X * tileSize,
            rect.Y * tileSize,
            rect.Width * tileSize,
            rect.Height * tileSize
        );

        using var bgPen = new Pen(Color.Black, 2f);
        g.DrawRectangle(bgPen, pxRect);

        using var pen = new Pen(Color.LimeGreen, 2f);

        if (_isSelectionCopied)
        {
            pen.DashStyle = DashStyle.Custom;
            pen.DashPattern = [4f, 4f];
            pen.DashOffset = _dashOffset;
        }

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
        GC.SuppressFinalize(this);
    }
}
