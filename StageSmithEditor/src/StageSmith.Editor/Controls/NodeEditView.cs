using StageSmith.Core.Models;

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
    private const float _zoomMin = 0.25f;
    private const float _zoomMax = 4.0f;
    private const float _zoomStep = 0.25f;

    //========================
    // ノードサイズ（基準 1.0x）
    //========================
    private const int _nodeW = 32;
    private const int _nodeH = 30;
    private const int _nodeGap = 4; // ノード間の隙間

    //========================
    // 選択・ホバー状態
    //========================
    public int SelectedPageIndex { get; private set; } = -1;
    private Point _hoveredGridPos = new(-1, -1);

    //========================
    // ドラッグ判定
    //========================
    private Point _mouseDownPos;
    private bool _isDragging;
    private const int _dragThreshold = 4;

    //========================
    // 右クリック時のヒットテスト結果保持
    //========================
    private int _contextMenuTargetPageIndex = -1;

    //========================
    // 初期化
    //========================
    public NodeEditView(EditorContext context)
    {
        _context = context;

        DoubleBuffered = true;
        BackColor = Color.FromArgb(40, 40, 45);

        MouseDown += OnMouseDown;
        MouseMove += OnMouseMove;
        MouseUp += OnMouseUp;
        MouseWheel += OnMouseWheel;
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

        // TODO: Z座標フィルタを PageNodeEditorForm.SelectedZ から取得する
        // 現時点では全ページを描画する（Z対応は後続タスク）

        foreach (var page in stage.Pages)
        {
            DrawNode(g, page, stage.Pages.IndexOf(page));
        }
    }

    private void DrawNode(Graphics g, Page page, int pageIndex)
    {
        var gridPos = GetGridPosition(page);
        var rect = GridToRect(gridPos);

        // ノード色を決定
        var color = GetNodeColor(pageIndex, page);

        using var brush = new SolidBrush(color);
        g.FillRectangle(brush, rect);

        using var pen = new Pen(Color.FromArgb(80, 80, 90), 1);
        g.DrawRectangle(pen, rect);

        // ページ番号テキスト（1.5x未満のとき）
        if (_zoom < 1.5f)
        {
            using var font = new Font("Yu Gothic UI", 7f * _zoom);
            using var textBrush = new SolidBrush(Color.White);
            var text = pageIndex.ToString();
            var textSize = g.MeasureString(text, font);
            var textPos = new PointF(
                rect.X + (rect.Width - textSize.Width) / 2,
                rect.Y + (rect.Height - textSize.Height) / 2
            );
            g.DrawString(text, font, textBrush, textPos);
        }
        // TODO: 1.5x以上のときタイルプレビューを描画する（後続タスク）
    }

    private Color GetNodeColor(int pageIndex, Page page)
    {
        if (pageIndex == SelectedPageIndex)
            return Color.Yellow;

        if (!page.Enable)
            return Color.Gray;

        // 隣接接続がすべて未設定（0xFF）かチェック
        var h = page.Header;
        var hasAnyConnection =
            h.LeftPage != 0xFF ||
            h.RightPage != 0xFF ||
            h.UpPage != 0xFF ||
            h.DownPage != 0xFF;

        return hasAnyConnection
            ? Color.FromArgb(100, 180, 100)  // 設定済み（緑）
            : Color.FromArgb(180, 80, 80);  // 隣接未接続（赤）
    }

    //========================
    // 座標変換
    //========================

    /// <summary>ページのグリッド座標を取得する（暫定：ページインデックスを仮配置）。</summary>
    private static Point GetGridPosition(Page page)
    {
        // TODO: ノードエディタのXY配置情報をPageまたはNodeデータに持たせる
        // 暫定として仮の配置を返す
        return new Point(0, 0);
    }

    /// <summary>グリッド座標をスクリーン上の描画矩形に変換する。</summary>
    private RectangleF GridToRect(Point gridPos)
    {
        var step = (_nodeW + _nodeGap) * _zoom;
        var x = _viewOffset.X + gridPos.X * step;
        var y = _viewOffset.Y + gridPos.Y * step;

        return new RectangleF(x, y, _nodeW * _zoom, _nodeH * _zoom);
    }

    /// <summary>スクリーン座標をグリッド座標に変換する。</summary>
    private Point ScreenToGrid(Point screenPos)
    {
        var step = (_nodeW + _nodeGap) * _zoom;
        var gx = (int)Math.Floor((screenPos.X - _viewOffset.X) / step);
        var gy = (int)Math.Floor((screenPos.Y - _viewOffset.Y) / step);
        return new Point(gx, gy);
    }

    /// <summary>スクリーン座標のページインデックスを返す。なければ-1。</summary>
    private int HitTestPageIndex(Point screenPos)
    {
        var stage = _context.CurrentStage;
        if (stage == null) return -1;

        for (var i = 0; i < stage.Pages.Count; i++)
        {
            var rect = GridToRect(GetGridPosition(stage.Pages[i]));
            if (rect.Contains(screenPos))
                return i;
        }

        return -1;
    }

    //========================
    // マウス操作
    //========================
    private void OnMouseDown(object? sender, MouseEventArgs e)
    {
        _mouseDownPos = e.Location;
        _isDragging = false;

        if (e.Button == MouseButtons.Right)
        {
            // 右クリック時はヒットテスト結果を保持しておく
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

            if (!_isDragging && Math.Abs(dx) + Math.Abs(dy) >= _dragThreshold)
                _isDragging = true;

            if (_isDragging)
            {
                _viewOffset.X += dx;
                _viewOffset.Y += dy;
                _mouseDownPos = e.Location;
                Invalidate();
            }
        }
    }

    private void OnMouseUp(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left && !_isDragging)
        {
            // クリック確定 → ページ選択
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
        var pos = PointToClient(Cursor.Position);
        var pageIndex = HitTestPageIndex(pos);

        if (pageIndex >= 0)
            PageDoubleClick?.Invoke(pageIndex);
    }

    private void OnMouseWheel(object? sender, MouseEventArgs e)
    {
        if (ModifierKeys.HasFlag(Keys.Control))
        {
            // Ctrl + ホイール → ズーム
            var delta = e.Delta > 0 ? _zoomStep : -_zoomStep;
            _zoom = Math.Clamp(_zoom + delta, _zoomMin, _zoomMax);
            Invalidate();
        }
        else
        {
            // 通常ホイール → スクロール
            _viewOffset.Y += e.Delta > 0 ? 40 : -40;
            Invalidate();
        }
    }
}
