using StageSmith.Core.Models;
using System.ComponentModel;

namespace StageSmith.Editor;

/// <summary>
/// ノードエディタの中央描画領域。
/// OnPaintによるカスタム描画でノードを表示する。
/// </summary>
public class NodeEditView : Panel
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
    // 右クリック時のヒットテスト結果保持
    //========================
    private int _contextMenuTargetPageIndex = -1;

    //========================
    // Z座標フィルタ（PageNodeEditorFormから設定される）
    //========================
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int FilterZ { get; set; } = 0;

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

    //========================
    // 公開メソッド
    //========================

    /// <summary>
    /// MainFormからページ切り替えが通知されたとき、選択状態を同期する。
    /// </summary>
    public void SetSelectedPage(int pageIndex)
    {
        SelectedPageIndex = pageIndex;
        Invalidate();
    }

    /// <summary>
    /// 右クリック時のヒットテスト。コンテキストメニューの状態切り替えに使用する。
    /// </summary>
    public int? HitTestPage()
    {
        return _contextMenuTargetPageIndex >= 0
            ? _contextMenuTargetPageIndex
            : null;
    }

    //========================
    // 描画
    //========================
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        var g = e.Graphics;
        g.Clear(BackColor);

        var stage = _context.CurrentStage;
        if (stage == null) return;

        // 選択中のZ階層に属するページのみ描画
        for (var i = 0; i < stage.Pages.Count; i++)
        {
            var page = stage.Pages[i];
            if (page.Header.Z != FilterZ) continue;
            DrawNode(g, page, i);
        }

        // 仮ページ（候補位置）を描画
        DrawCandidateNodes(g, stage);
    }

    private void DrawNode(Graphics g, Page page, int pageIndex)
    {
        var rect  = PageToRect(page);
        var color = GetNodeColor(pageIndex, page);

        using var brush = new SolidBrush(color);
        g.FillRectangle(brush, rect);

        using var borderPen = new Pen(Color.FromArgb(80, 80, 90), 1);
        g.DrawRectangle(borderPen, rect);

        // 1.5x未満 → 色 + ページ番号テキスト
        if (_zoom < 1.5f)
        {
            DrawNodeText(g, rect, pageIndex.ToString());
        }
        // TODO: 1.5x以上 → タイルプレビュー（後続タスク）
    }

    /// <summary>
    /// 設定済みページの隣に仮ページ（候補位置）を描画する。
    /// </summary>
    private void DrawCandidateNodes(Graphics g, Stage stage)
    {
        // 設定済みページのグリッド座標セットを収集
        var occupied = new HashSet<Point>();
        foreach (var page in stage.Pages)
        {
            if (page.Header.Z == FilterZ)
                occupied.Add(new Point(page.NodeX, page.NodeY));
        }

        // 各設定済みページの上下左右に空きがあれば候補位置を描画
        var candidates = new HashSet<Point>();
        foreach (var pos in occupied)
        {
            foreach (var neighbor in GetNeighborPositions(pos))
            {
                if (!occupied.Contains(neighbor))
                    candidates.Add(neighbor);
            }
        }

        using var brush = new SolidBrush(Color.FromArgb(30, 255, 255, 255));
        using var pen   = new Pen(Color.FromArgb(80, 255, 255, 255), 1);

        foreach (var candidate in candidates)
        {
            var rect = GridToRect(candidate);
            g.FillRectangle(brush, rect);
            g.DrawRectangle(pen, rect);
        }
    }

    private static IEnumerable<Point> GetNeighborPositions(Point pos)
    {
        yield return new Point(pos.X - 1, pos.Y);
        yield return new Point(pos.X + 1, pos.Y);
        yield return new Point(pos.X,     pos.Y - 1);
        yield return new Point(pos.X,     pos.Y + 1);
    }

    private void DrawNodeText(Graphics g, RectangleF rect, string text)
    {
        var fontSize = Math.Max(6f, 7f * _zoom);
        using var font      = new Font("Yu Gothic UI", fontSize);
        using var textBrush = new SolidBrush(Color.White);

        var textSize = g.MeasureString(text, font);
        var textPos  = new PointF(
            rect.X + (rect.Width  - textSize.Width)  / 2,
            rect.Y + (rect.Height - textSize.Height) / 2
        );

        g.DrawString(text, font, textBrush, textPos);
    }

    private Color GetNodeColor(int pageIndex, Page page)
    {
        if (pageIndex == SelectedPageIndex)
            return Color.Yellow;

        if (!page.Enable)
            return Color.Gray;

        var h = page.Header;
        var hasAnyConnection =
            h.LeftPage  != 0xFF ||
            h.RightPage != 0xFF ||
            h.UpPage    != 0xFF ||
            h.DownPage  != 0xFF;

        return hasAnyConnection
            ? Color.FromArgb(100, 180, 100)  // 設定済み（緑）
            : Color.FromArgb(180, 80,  80);  // 隣接未接続（赤）
    }

    //========================
    // 座標変換
    //========================

    /// <summary>PageのNodeX/NodeYからスクリーン上の描画矩形を返す。</summary>
    private RectangleF PageToRect(Page page)
        => GridToRect(new Point(page.NodeX, page.NodeY));

    /// <summary>グリッド座標をスクリーン上の描画矩形に変換する。</summary>
    private RectangleF GridToRect(Point gridPos)
    {
        var step = (NodeW + NodeGap) * _zoom;
        var x    = _viewOffset.X + gridPos.X * step;
        var y    = _viewOffset.Y + gridPos.Y * step;
        return new RectangleF(x, y, NodeW * _zoom, NodeH * _zoom);
    }

    /// <summary>スクリーン座標をグリッド座標に変換する。</summary>
    private Point ScreenToGrid(Point screenPos)
    {
        var step = (NodeW + NodeGap) * _zoom;
        var gx   = (int)Math.Floor((screenPos.X - _viewOffset.X) / step);
        var gy   = (int)Math.Floor((screenPos.Y - _viewOffset.Y) / step);
        return new Point(gx, gy);
    }

    /// <summary>スクリーン座標のページインデックスを返す。なければ-1。</summary>
    private int HitTestPageIndex(Point screenPos)
    {
        var stage = _context.CurrentStage;
        if (stage == null) return -1;

        for (var i = 0; i < stage.Pages.Count; i++)
        {
            var page = stage.Pages[i];
            if (page.Header.Z != FilterZ) continue;

            var rect = PageToRect(page);
            if (rect.Contains(screenPos))
                return i;
        }

        return -1;
    }

    /// <summary>スクリーン座標が候補位置に当たるかチェックしグリッド座標を返す。なければnull。</summary>
    private Point? HitTestCandidate(Point screenPos)
    {
        var stage = _context.CurrentStage;
        if (stage == null) return null;

        var occupied = new HashSet<Point>(
            stage.Pages
                .Where(p => p.Header.Z == FilterZ)
                .Select(p => new Point(p.NodeX, p.NodeY))
        );

        foreach (var pos in occupied)
        {
            foreach (var neighbor in GetNeighborPositions(pos))
            {
                if (occupied.Contains(neighbor)) continue;
                if (GridToRect(neighbor).Contains(screenPos))
                    return neighbor;
            }
        }

        return null;
    }

    //========================
    // マウス操作
    //========================
    private void OnMouseDown(object? sender, MouseEventArgs e)
    {
        _mouseDownPos = e.Location;
        _isDragging   = false;

        if (e.Button == MouseButtons.Right)
        {
            _contextMenuTargetPageIndex = HitTestPageIndex(e.Location);
        }
    }

    private void OnMouseMove(object? sender, MouseEventArgs e)
    {
        // ホバー座標更新
        var grid = ScreenToGrid(e.Location);
        if (grid != _hoveredGridPos)
        {
            _hoveredGridPos = grid;
            CoordChanged?.Invoke(grid.X, grid.Y);
        }

        if (e.Button == MouseButtons.Left)
        {
            var dx = e.X - _mouseDownPos.X;
            var dy = e.Y - _mouseDownPos.Y;

            if (!_isDragging && Math.Abs(dx) + Math.Abs(dy) >= DragThreshold)
                _isDragging = true;

            if (_isDragging)
            {
                _viewOffset.X += dx;
                _viewOffset.Y += dy;
                _mouseDownPos  = e.Location;
                Invalidate();
            }
        }
    }

    private void OnMouseUp(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left && !_isDragging)
        {
            var pageIndex = HitTestPageIndex(e.Location);
            if (pageIndex >= 0)
            {
                SelectedPageIndex = pageIndex;
                PageSelected?.Invoke(pageIndex);
                Invalidate();
            }
        }

        _isDragging = false;
    }

    private void OnDoubleClick(object? sender, EventArgs e)
    {
        var pos       = PointToClient(Cursor.Position);
        var pageIndex = HitTestPageIndex(pos);

        if (pageIndex >= 0)
            PageDoubleClick?.Invoke(pageIndex);
    }

    private void OnMouseWheel(object? sender, MouseEventArgs e)
    {
        if (ModifierKeys.HasFlag(Keys.Control))
        {
            var delta = e.Delta > 0 ? ZoomStep : -ZoomStep;
            _zoom = Math.Clamp(_zoom + delta, ZoomMin, ZoomMax);
            Invalidate();
        }
        else
        {
            _viewOffset.Y += e.Delta > 0 ? 40 : -40;
            Invalidate();
        }
    }
}
