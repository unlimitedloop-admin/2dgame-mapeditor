using System.Drawing.Drawing2D;
using Timer = System.Windows.Forms.Timer;

namespace StageSmith.Editor.Tools;

public class SelectionTool : ITool, IDisposable
{
    // ===== 選択状態 =====
    private Point? _selectionStart;
    private Point? _selectionEnd;

    public Rectangle? SelectionRect { get; private set; }

    // ===== マーチングアント =====
    private readonly Timer _marchTimer = new() { Interval = 80 };
    private float _dashOffset = 0f;

    public float DashOffset => _dashOffset;

    // ===== コピー状態 =====
    private bool _isSelectionCopied = false;
    public bool IsSelectionCopied => _isSelectionCopied;

    // ===== 通知 =====
    public event Action? SelectionChanged;

    public SelectionTool()
    {
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
        StopMarching();

        _isSelectionCopied = false;

        _selectionStart = new Point(x, y);
        _selectionEnd = _selectionStart;

        UpdateSelectionRect();
        SelectionChanged?.Invoke();
    }

    public void OnMouseMove(int x, int y)
    {
        if (!_selectionStart.HasValue)
            return;

        _selectionEnd = new Point(x, y);
        UpdateSelectionRect();

        SelectionChanged?.Invoke();
    }

    public void OnMouseUp(int x, int y)
    {
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

        SelectionRect = new Rectangle(
            left,
            top,
            right - left + 1,
            bottom - top + 1
        );
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

        using var pen = new Pen(Color.YellowGreen, 2f);

        if (_isSelectionCopied)
        {
            pen.DashStyle = DashStyle.Custom;
            pen.DashPattern = [4f, 4f];
            pen.DashOffset = _dashOffset;
        }

        g.DrawRectangle(pen, pxRect);
    }

    public void Dispose()
    {
        _marchTimer.Dispose();
    }
}
