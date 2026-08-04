using StageSmith.Core.Constants;
using StageSmith.Core.Models;
using StageSmith.Editor.Utilities;
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
    private int   _dragSourcePageIndex = -1;   // ドラッグ中のページインデックス
    private bool  _isDraggingNode      = false; // ノードD&D中か
    private bool  _isDraggingNodeCopy  = false; // 複製D&D中か（Ctrl+Shift）
    private Point? _dragCurrentGridPos = null; // 現在のグリッド座標（ゴースト描画用）

    /// <summary>ノード移動が確定したとき発火する。PageNodeEditorFormがStageManagerを更新する。</summary>
    public event Action<int, int, int>? NodeMoved;   // (pageIndex, newX, newY)

    /// <summary>ノード複製が確定したとき発火する。</summary>
    public event Action<int, int, int>? NodeCopied;  // (pageIndex, newX, newY)

    //========================
    // 複製モード（コンテキストメニュー「このページを複製」）
    //========================
    private bool _isPasteMode         = false; // 複製モード中か
    private int _pasteModeSourceIndex = -1;   // 複製元ページインデックス
    private Point? _pasteHoverGridPos = null;  // ホバー中のグリッド座標

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
    private readonly Dictionary<Guid, Bitmap> _previewCache = [];

    // プレビュー表示切り替えの閾値
    private const float PreviewZoomThreshold = 1.5f;

    // プレビュー生成サイズ（1.5x時のノードサイズに合わせる）
    private const int PreviewW = (int)(NodeW * PreviewZoomThreshold);
    private const int PreviewH = (int)(NodeH * PreviewZoomThreshold);

    /// <summary>
    /// タイルセット画像を設定し、全ページのプレビューを一括生成する。
    /// プロジェクト読み込み時・タイルセット変更時に呼び出す。
    /// </summary>
    public void SetTileset(Bitmap? tileset, IReadOnlyList<Page>? pages)
    {
        _tilesetHolder.Replace(tileset);
        RebuildAllPreviews(pages);
    }

    /// <summary>
    /// 指定ページのプレビューキャッシュを再生成する。
    /// ページ編集後に呼び出す。
    /// </summary>
    public void InvalidatePageCache(Guid pageId, Page page)
    {
        if (_previewCache.TryGetValue(pageId, out var old))
        {
            old.Dispose();
            _previewCache.Remove(pageId);
        }

        var tileset = _tilesetHolder.Current;
        if (tileset != null)
            _previewCache[pageId] = BuildPreview(page, tileset);

        Invalidate();
    }

    private void RebuildAllPreviews(IReadOnlyList<Page>? pages)
    {
        foreach (var bmp in _previewCache.Values)
            bmp.Dispose();

        _previewCache.Clear();

        var tileset = _tilesetHolder.Current;
        if (tileset == null || pages == null) return;

        foreach (var page in pages)
            _previewCache[page.Id] = BuildPreview(page, tileset);
    }

    /// <summary>
    /// 1ページ分のプレビューBitmapを生成する。
    /// タイルマップをPreviewW x PreviewHに縮小描画する。
    /// </summary>
    private static Bitmap BuildPreview(Page page, Bitmap tileset)
    {
        var bmp = new Bitmap(PreviewW, PreviewH);
        using var g = Graphics.FromImage(bmp);
        g.Clear(Color.FromArgb(30, 30, 35));
        g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;

        var tileMap  = page.TileMap;
        var tileW    = tileMap.Width;
        var tileH    = tileMap.Height;
        var cellW    = (float)PreviewW / tileW;
        var cellH    = (float)PreviewH / tileH;

        // タイルセットの1タイルサイズ（16x16固定）
        const int SrcTile = 16;
        var tilesPerRow = tileset.Width / SrcTile;

        for (var y = 0; y < tileH; y++)
        {
            for (var x = 0; x < tileW; x++)
            {
                var tileId = tileMap.GetTile(x, y);
                if (tileId == 0) continue;

                var srcX = (tileId % tilesPerRow) * SrcTile;
                var srcY = (tileId / tilesPerRow) * SrcTile;
                var src  = new Rectangle(srcX, srcY, SrcTile, SrcTile);
                var dst  = new RectangleF(x * cellW, y * cellH, cellW, cellH);

                g.DrawImage(tileset, dst, src, GraphicsUnit.Pixel);
            }
        }

        return bmp;
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
            foreach (var bmp in _previewCache.Values)
                bmp.Dispose();
            _previewCache.Clear();

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

        // ノードD&D中のゴースト描画
        if (_isDraggingNode && _dragCurrentGridPos.HasValue)
            DrawDragGhost(g);

        // 複製モード中のハイライト描画
        if (_isPasteMode)
            DrawPasteModeOverlay(g, stage);
    }

    /// <summary>複製モード中にホバー位置をハイライト描画する。</summary>
    private void DrawPasteModeOverlay(Graphics g, Stage stage)
    {
        if (!_pasteHoverGridPos.HasValue) return;

        var rect = GridToRect(_pasteHoverGridPos.Value);

        // 複製元を太いオレンジ枠で強調
        if (_pasteModeSourceIndex >= 0)
        {
            var sourcePage = stage.Pages.ElementAtOrDefault(_pasteModeSourceIndex);
            if (sourcePage != null)
            {
                var sourceRect = PageToRect(sourcePage);
                using var sourcePen = new Pen(Color.Orange, 3);
                g.DrawRectangle(sourcePen, sourceRect);
            }
        }

        // ホバー中の貼り付け先をシアン枠でハイライト
        using var hoverBrush = new SolidBrush(Color.FromArgb(60, 0, 255, 220));
        using var hoverPen   = new Pen(Color.Cyan, 2);
        g.FillRectangle(hoverBrush, rect);
        g.DrawRectangle(hoverPen, rect);
    }

    /// <summary>ノードD&D中にドロップ先グリッドにゴーストを描画する。</summary>
    private void DrawDragGhost(Graphics g)
    {
        var rect  = GridToRect(_dragCurrentGridPos!.Value);
        var color = _isDraggingNodeCopy
            ? Color.FromArgb(120, 100, 180, 255)  // 複製：青系半透明
            : Color.FromArgb(120, 255, 200, 0);   // 移動：黄系半透明

        using var brush = new SolidBrush(color);
        using var pen   = new Pen(_isDraggingNodeCopy ? Color.CornflowerBlue : Color.Yellow, 2);

        g.FillRectangle(brush, rect);
        g.DrawRectangle(pen, rect);
    }

    private void DrawNode(Graphics g, Page page, int pageIndex)
    {
        var rect  = PageToRect(page);
        var color = GetNodeColor(pageIndex, page);

        using var brush = new SolidBrush(color);
        g.FillRectangle(brush, rect);

        if (ShowPreview && _zoom >= PreviewZoomThreshold && _previewCache.TryGetValue(page.Id, out var preview))
        {
            // 1.5x以上 → タイルプレビュー画像をノード内に描画
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
            g.DrawImage(preview, rect);

            // 選択中は色オーバーレイを薄く重ねて分かるようにする
            if (pageIndex == SelectedPageIndex)
            {
                using var overlay = new SolidBrush(Color.FromArgb(80, 255, 255, 0));
                g.FillRectangle(overlay, rect);
            }
        }
        else
        {
            // 1.5x未満 → 色 + Room IDテキスト
            var textColor = IsLightColor(color) ? Color.Black : Color.White;
            DrawNodeText(g, rect, NumberFormatHelper.FormatByte(page.Header.RoomId, _numberDisplayFormat), textColor);
        }

        // 枠線は常に描画
        using var borderPen = new Pen(
            pageIndex == SelectedPageIndex ? Color.Yellow : Color.FromArgb(80, 80, 90), 1);
        g.DrawRectangle(borderPen, rect);
    }

    /// <summary>
    /// 明るい色かどうかを輝度で判定する。
    /// 輝度が高い（明るい）場合はtrueを返す。
    /// </summary>
    private static bool IsLightColor(Color color)
        => (color.R * 0.299 + color.G * 0.587 + color.B * 0.114) > 128;

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

    private void DrawNodeText(Graphics g, RectangleF rect, string text, Color textColor)
    {
        var fontSize = Math.Max(6f, 7f * _zoom);
        using var font      = new Font("Yu Gothic UI", fontSize);
        using var textBrush = new SolidBrush(textColor);

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
        _isDragging      = false;
        _isDraggingNode  = false;
        _isDraggingNodeCopy = false;
        _dragSourcePageIndex = -1;

        if (e.Button == MouseButtons.Right)
        {
            _contextMenuTargetPageIndex = HitTestPageIndex(e.Location);
            _contextMenuTargetCandidate = _contextMenuTargetPageIndex < 0
                ? HitTestCandidate(e.Location)
                : null;
        }
        else if (e.Button == MouseButtons.Left && ModifierKeys.HasFlag(Keys.Control))
        {
            // Ctrl押下中 → ノードD&D候補
            _dragSourcePageIndex = HitTestPageIndex(e.Location);
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

        // 複製モード中はホバー位置を更新して再描画
        if (_isPasteMode)
        {
            var hoverPage      = HitTestPageIndex(e.Location);
            var hoverCandidate = hoverPage < 0 ? HitTestCandidate(e.Location) : null;

            Point? newHover;
            if (hoverPage >= 0)
            {
                var stage = _context.CurrentStage;
                var page  = stage?.Pages.ElementAtOrDefault(hoverPage);
                newHover  = page != null ? new Point(page.NodeX, page.NodeY) : null;
            }
            else if (hoverCandidate.HasValue)
            {
                newHover = hoverCandidate.Value;
            }
            else
            {
                newHover = null;
            }

            if (newHover != _pasteHoverGridPos)
            {
                _pasteHoverGridPos = newHover;
                Invalidate();
            }
            return;
        }

        if (e.Button == MouseButtons.Left)
        {
            var dx = e.X - _mouseDownPos.X;
            var dy = e.Y - _mouseDownPos.Y;
            var moved = Math.Abs(dx) + Math.Abs(dy) >= DragThreshold;

            if (ModifierKeys.HasFlag(Keys.Control) && _dragSourcePageIndex >= 0)
            {
                // Ctrl+ドラッグ → ノードD&D
                if (!_isDraggingNode && moved)
                {
                    _isDraggingNode     = true;
                    _isDraggingNodeCopy = ModifierKeys.HasFlag(Keys.Shift);
                }

                if (_isDraggingNode)
                {
                    _dragCurrentGridPos = grid;
                    Invalidate();
                }
            }
            else
            {
                // 通常ドラッグ → ビュー移動
                if (!_isDragging && moved)
                    _isDragging = true;

                if (_isDragging)
                {
                    _viewOffset.X += dx;
                    _viewOffset.Y += dy;
                    _mouseDownPos  = e.Location;
                    ViewOffsetChanged?.Invoke(_viewOffset);
                    Invalidate();
                }
            }
        }
    }

    private void OnMouseUp(object? sender, MouseEventArgs e)
    {
        // 複製モード中の左クリック → 貼り付け先確定
        if (_isPasteMode && e.Button == MouseButtons.Left)
        {
            if (_pasteHoverGridPos.HasValue)
            {
                // 貼り付け先が既存ページか候補位置かを判定
                var targetPageIndex = HitTestPageIndex(e.Location);
                PasteModeConfirmed?.Invoke(
                    _pasteModeSourceIndex,
                    targetPageIndex,
                    _pasteHoverGridPos.Value.X,
                    _pasteHoverGridPos.Value.Y);
            }
            return;
        }

        if (e.Button == MouseButtons.Left)
        {
            if (_isDraggingNode && _dragSourcePageIndex >= 0)
            {
                // ノードD&D確定
                var targetGrid = ScreenToGrid(e.Location);
                if (_isDraggingNodeCopy)
                    NodeCopied?.Invoke(_dragSourcePageIndex, targetGrid.X, targetGrid.Y);
                else
                    NodeMoved?.Invoke(_dragSourcePageIndex, targetGrid.X, targetGrid.Y);
            }
            else if (!_isDragging)
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
        }

        _isDragging         = false;
        _isDraggingNode     = false;
        _isDraggingNodeCopy = false;
        _dragSourcePageIndex = -1;
        _dragCurrentGridPos  = null;
        Invalidate();
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
            // Ctrl + ホイール → ズーム
            var delta = e.Delta > 0 ? ZoomStep : -ZoomStep;
            _zoom = Math.Clamp(_zoom + delta, ZoomMin, ZoomMax);
            ZoomChanged?.Invoke(_zoom);
            Invalidate();
        }
        else if (ModifierKeys.HasFlag(Keys.Shift))
        {
            // Shift + ホイール → 左右スクロール
            _viewOffset.X += e.Delta > 0 ? 40 : -40;
            ViewOffsetChanged?.Invoke(_viewOffset);
            Invalidate();
        }
        else
        {
            // 通常ホイール → 上下スクロール
            _viewOffset.Y += e.Delta > 0 ? 40 : -40;
            ViewOffsetChanged?.Invoke(_viewOffset);
            Invalidate();
        }
    }
}
