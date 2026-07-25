using StageSmith.Core.Constants;
using StageSmith.Core.Models;
using StageSmith.Editor.Constants;
using StageSmith.Editor.Tools;
using StageSmith.Editor.Utilities;
using System.ComponentModel;

namespace StageSmith.Editor.Controls;

public class MapViewControl : DoubleBufferedPanel
{
    private TileMap? _tileMap;
    private readonly SafeTilesetHolder _tilesetHolder = new();

    // ModifierKeysの状態をポーリングして、スポイト/バケツのカーソルを更新するためのタイマー
    private readonly System.Windows.Forms.Timer _modifierPollTimer = new() { Interval = 50 };
    private bool _lastIsAlt = false;
    private bool _lastIsShift = false;

    private bool _showGrid = true;

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
        };
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _tilesetHolder.Dispose();
            _modifierPollTimer.Dispose();
        }

        base.Dispose(disposing);
    }

    // ===== Toggle Grid =====
    private bool _showRowNumbers = false;
    private bool _showColumnNumbers = false;

    public bool ShowRowNumbers => _showRowNumbers;
    public bool ShowColumnNumbers => _showColumnNumbers;

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

    private int OffsetX =>
        ViewerConstants.MapViewMargin + (_showRowNumbers ? ViewerConstants.RowNumberBandWidth : 0);

    private int OffsetY =>
        ViewerConstants.MapViewMargin + (_showColumnNumbers ? ViewerConstants.ColumnNumberBandHeight : 0);

    // ===== Zoom =====
    private const float MinZoomScale = 0.5f;
    private const float DefaultZoomScale = 1.0f;
    private const float MaxZoomScale = 2.0f;
    private const float ZoomStep = 0.5f;

    private float _zoomScale = DefaultZoomScale;

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public float ZoomScale => _zoomScale;

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int CurrentTileRenderSize
        => Math.Max(1, (int)MathF.Round(ViewerConstants.TileRenderSize * _zoomScale));

    public bool CanZoomIn => _zoomScale < MaxZoomScale;
    public bool CanZoomOut => _zoomScale > MinZoomScale;
    public bool IsDefaultZoom => Math.Abs(_zoomScale - DefaultZoomScale) < 0.001f;

    public event EventHandler? ZoomChanged;

    private Point _hoverTile = new(-1, -1);

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

    private void OnToolChanged(ITool? tool)
    {
        Cursor = Cursors.Default;
        Invalidate();
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

    // ===== Adjacent Navigation =====
    public event EventHandler<TileContextMenuEventArgs>? ContextMenuRequested;
    public event EventHandler<AdjacentNavigationRequestedEventArgs>? AdjacentNavigationRequested;

    private MapAdjacentState _adjacentState = new();

    public void SetAdjacentState(MapAdjacentState state)
    {
        _adjacentState = state;
        Invalidate();
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

    public Point GetHoverTile() => _hoverTile;

    // =========================
    // 描画
    // =========================
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        if (_tileMap == null) return;

        var g = e.Graphics;

        // ピクセルアート向けに最近傍補間を使用する。
        // デフォルトのバイリニア補間だと拡大時に隣接タイルが滲んで混入するため。
        g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
        g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;

        g.Clear(BackColor);

        if (GetUsableTileset() != null)
        {
            DrawTiles(g);
        }

        if (_showGrid)
        {
            DrawGrid(g);
        }

        DrawColumnNumbers(g);
        DrawRowNumbers(g);

        DrawSearchHighlights(g);
        DrawPreview(g);

        // SelectionToolに描かせる
        SelectionTool?.DrawOverlay(g, CurrentTileRenderSize, OffsetX, OffsetY);
        SelectionTool?.DrawMovingOverlay(g, CurrentTileRenderSize, GetUsableTileset(), OffsetX, OffsetY);

        DrawAdjacentNavigationButtons(g);

        // Extended add plus cursor at copy mode
        if (SelectionTool?.IsCopyModeActive() == true)
        {
            DrawPlusCursor(g);
        }
    }

    private void DrawTiles(Graphics g)
    {
        var tileset = GetUsableTileset();
        if (_tileMap == null || tileset == null) return;

        var srcSize = MapConstants.DefaultTileSize;   // 16: 画像の切り出しサイズ
        var dstSize = CurrentTileRenderSize; // 32: 画面上の描画サイズ
        var offsetX = OffsetX;
        var offsetY = OffsetY;
        var tilesPerRow = tileset.Width / srcSize;

        for (var y = 0; y < _tileMap.Height; y++)
        {
            for (var x = 0; x < _tileMap.Width; x++)
            {
                var tileId = _tileMap.GetTile(x, y);

                var sx = (tileId % tilesPerRow) * srcSize;
                var sy = (tileId / tilesPerRow) * srcSize;

                var srcRect = new Rectangle(sx, sy, srcSize, srcSize);
                var dstRect = new Rectangle(
                    offsetX + x * dstSize,
                    offsetY + y * dstSize,
                    dstSize,
                    dstSize);

                g.DrawImage(tileset, dstRect, srcRect, GraphicsUnit.Pixel);
            }
        }
    }

    private void DrawGrid(Graphics g)
    {
        if (_tileMap == null) return;

        var dstSize = CurrentTileRenderSize;
        var offsetX = OffsetX;
        var offsetY = OffsetY;

        using var pen = new Pen(Color.FromArgb(80, Color.White));

        for (var x = 0; x <= _tileMap.Width; x++)
        {
            var px = offsetX + x * dstSize;
            g.DrawLine(pen, px, offsetY, px, offsetY + _tileMap.Height * dstSize);
        }

        for (var y = 0; y <= _tileMap.Height; y++)
        {
            var py = offsetY + y * dstSize;
            g.DrawLine(pen, offsetX, py, offsetX + _tileMap.Width * dstSize, py);
        }
    }

    private void DrawPreview(Graphics g)
    {
        if (!_showPreview) return;
        if (GetUsableTileset() == null) return;
        if (_hoverTile.X < 0 || _hoverTile.Y < 0) return;

        if (PreviewMetaTile != null)
        {
            DrawMetaTilePreview(g, PreviewMetaTile);
            return;
        }

        DrawTilePreview(g);
    }

    private void DrawTilePreview(Graphics g)
    {
        var tileset = GetUsableTileset();
        if (tileset == null || PreviewTileId < 0) return;

        var srcSize = MapConstants.DefaultTileSize;   // 16: 画像の切り出しサイズ
        var dstSize = CurrentTileRenderSize; // 32: 画面上の描画サイズ
        var tilesPerRow = tileset.Width / srcSize;

        var sx = (PreviewTileId % tilesPerRow) * srcSize;
        var sy = (PreviewTileId / tilesPerRow) * srcSize;

        var srcRect = new Rectangle(sx, sy, srcSize, srcSize);
        var dstRect = new Rectangle(
            OffsetX + _hoverTile.X * dstSize,
            OffsetY + _hoverTile.Y * dstSize,
            dstSize,
            dstSize
        );

        DrawImageTransparent(g, srcRect, dstRect);

        using var pen = new Pen(Color.Yellow, 2);
        g.DrawRectangle(pen, dstRect);
    }

    private void DrawMetaTilePreview(Graphics g, MetaTile metaTile)
    {
        var tileset = GetUsableTileset();
        if (tileset == null) return;

        var srcSize = MapConstants.DefaultTileSize;
        var dstSize = CurrentTileRenderSize;
        var tilesPerRow = Math.Max(1, tileset.Width / srcSize);

        var minX = int.MaxValue;
        var minY = int.MaxValue;
        var maxX = int.MinValue;
        var maxY = int.MinValue;

        for (var y = 0; y < metaTile.Height; y++)
        {
            for (var x = 0; x < metaTile.Width; x++)
            {
                var tileId = metaTile.GetTile(x, y);

                if (tileId == MetaTile.EmptyTile)
                    continue;

                var mapX = _hoverTile.X + x;
                var mapY = _hoverTile.Y + y;

                // プレビューも実配置と同じく範囲外は無視する。
                if (!IsInside(mapX, mapY))
                    continue;

                var sx = (tileId % tilesPerRow) * srcSize;
                var sy = (tileId / tilesPerRow) * srcSize;

                var srcRect = new Rectangle(sx, sy, srcSize, srcSize);
                var dstRect = new Rectangle(
                    OffsetX + mapX * dstSize,
                    OffsetY + mapY * dstSize,
                    dstSize,
                    dstSize
                );

                DrawImageTransparent(g, srcRect, dstRect);

                minX = Math.Min(minX, dstRect.Left);
                minY = Math.Min(minY, dstRect.Top);
                maxX = Math.Max(maxX, dstRect.Right);
                maxY = Math.Max(maxY, dstRect.Bottom);
            }
        }

        if (minX == int.MaxValue)
            return;

        using var pen = new Pen(Color.Yellow, 2);
        g.DrawRectangle(
            pen,
            new Rectangle(minX, minY, maxX - minX - 1, maxY - minY - 1)
        );
    }

    private void DrawImageTransparent(Graphics g, Rectangle srcRect, Rectangle dstRect)
    {
        var tileset = GetUsableTileset();
        if (tileset == null) return;

        using var attr = new System.Drawing.Imaging.ImageAttributes();

        var matrix = new System.Drawing.Imaging.ColorMatrix
        {
            Matrix33 = 0.5f
        };

        attr.SetColorMatrix(matrix);

        g.DrawImage(
            tileset,
            dstRect,
            srcRect.X,
            srcRect.Y,
            srcRect.Width,
            srcRect.Height,
            GraphicsUnit.Pixel,
            attr
        );
    }

    private Bitmap? GetUsableTileset() => _tilesetHolder.Current;

    // =========================
    // 入力
    // =========================
    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);

        if (_tileMap == null) return;

        if (e.Button == MouseButtons.Left &&
            TryHitAdjacentNavigationButton(e.Location, out var direction))
        {
            var hasAdjacent = _adjacentState.HasAdjacent(direction);
            AdjacentNavigationRequested?.Invoke(
                this,
                new AdjacentNavigationRequestedEventArgs(direction, hasAdjacent)
            );
            return;
        }

        var (x, y) = ScreenToTile(e.X, e.Y);
        if (!IsInside(x, y)) return;

        if (e.Button == MouseButtons.Right)
        {
            return;
        }

        var isAlt = (ModifierKeys & Keys.Alt) != 0;

        // Alt+左クリックのスポイトは、MetaTileブラシ中でも常に機能させる。
        // （ShouldSuppressToolInputより前に判定することで迂回させる）
        if (e.Button == MouseButtons.Left && isAlt)
        {
            PickerTool?.OnMouseDown(x, y);
            return;
        }

        // MetaTileなど、外部側で左クリックを処理する特殊ブラシの場合、
        // MapViewControl内部のTool処理へ入力を渡さない（Alt+クリックは上で処理済みなのでここには来ない）。
        if (e.Button == MouseButtons.Left &&
            ShouldSuppressToolInput?.Invoke() == true)
        {
            return;
        }

        var isShift = (ModifierKeys & Keys.Shift) != 0;

        if (isShift)
        {
            // Selectionツール使用中は Shift+クリック＝選択範囲の対角拡張
            if (ReferenceEquals(_toolManager?.CurrentTool, SelectionTool))
            {
                SelectionTool?.ExtendSelection(x, y);
                Invalidate();
                return;
            }

            // それ以外（Penツールなど）は従来通りフィル
            FillTool?.OnMouseDown(x, y);
            Invalidate();
            return;
        }

        _toolManager?.CurrentTool?.OnMouseDown(x, y);
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);

        if (_tileMap == null) return;

        var (x, y) = ScreenToTile(e.X, e.Y);

        // ========================
        // 右クリック：コンテキストメニュー要求を発火
        // ========================
        if (e.Button == MouseButtons.Right)
        {
            if (IsInside(x, y))
            {
                ContextMenuRequested?.Invoke(
                    this,
                    new TileContextMenuEventArgs(x, y, PointToScreen(e.Location))
                );
            }
            return;
        }

        // NOTE: 範囲外ドロップを許可するため、ここではIsInsideチェックしない


        if (e.Button == MouseButtons.Left &&
            ShouldSuppressToolInput?.Invoke() == true)
        {
            return;
        }

        var isAlt = (ModifierKeys & Keys.Alt) != 0;

        if (isAlt)
        {
            PickerTool?.OnMouseUp(x, y);
            return;
        }

        _toolManager?.CurrentTool?.OnMouseUp(x, y);
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);

        if (_tileMap == null) return;

        var (x, y) = ScreenToTile(e.X, e.Y);

        if (!IsInside(x, y))
        {
            Cursor = Cursors.Default;
            _hoverTile = new Point(-1, -1);
            Invalidate();
            return;
        }

        var newHover = new Point(x, y);
        if (_hoverTile != newHover)
        {
            _hoverTile = newHover;
            Invalidate();
        }

        var currentTool = _toolManager?.CurrentTool;
        if (e.Button == MouseButtons.Left)
        {
            if (ShouldSuppressToolInput?.Invoke() != true)
            {
                currentTool?.OnMouseMove(x, y);
            }
        }
        Invalidate();
    }

    // =========================
    // 補助
    // =========================
    private (int x, int y) ScreenToTile(int px, int py)
    {
        var dstSize = CurrentTileRenderSize;
        return ((px - OffsetX) / dstSize, (py - OffsetY) / dstSize);
    }

    public bool TryScreenToTile(int px, int py, out int x, out int y)
    {
        (x, y) = ScreenToTile(px, py);
        return IsInside(x, y);
    }

    private bool IsInside(int x, int y)
    {
        return _tileMap != null &&
               x >= 0 && x < _tileMap.Width &&
               y >= 0 && y < _tileMap.Height;
    }

    private void DrawPlusCursor(Graphics g)
    {
        var pos = PointToClient(Cursor.Position);

        var size = 5;
        var offset = 24; // カーソルの位置よりも少し右下に描画するためのオフセット

        using var pen = new Pen(Color.White, 2);

        g.DrawLine(pen, pos.X - size + offset, pos.Y + offset, pos.X + size + offset, pos.Y + offset);
        g.DrawLine(pen, pos.X + offset, pos.Y - size + offset, pos.X + offset, pos.Y + size + offset);
    }

    public void UpdateCursor()
    {
        var isShift = (ModifierKeys & Keys.Shift) != 0;
        var isAlt = (ModifierKeys & Keys.Alt) != 0;

        var (x, y) = _hoverTile.X >= 0 ? (_hoverTile.X, _hoverTile.Y) : (-1, -1);

        if (x < 0 || y < 0)
        {
            Cursor = Cursors.Default;
            return;
        }

        var currentTool = _toolManager?.CurrentTool;

        // スポイト（Alt併用、右クリックの有無に関わらずカーソルで予告）
        if (isAlt)
        {
            Cursor = PickerTool?.GetCursor(x, y) ?? Cursors.Hand;
            return;
        }

        // バケツ（Pen限定）
        if (isShift && currentTool is PenTool)
        {
            Cursor = FillTool?.GetCursor(x, y) ?? Cursors.Hand;
            return;
        }

        Cursor = currentTool?.GetCursor(x, y) ?? Cursors.Default;
    }

    private void DrawAdjacentNavigationButtons(Graphics g)
    {
        foreach (var direction in new[]
        {
            PageDirection.Up,
            PageDirection.Down,
            PageDirection.Left,
            PageDirection.Right
        })
        {
            var rect = GetAdjacentNavigationButtonRect(direction);
            if (rect.IsEmpty)
                continue;

            var hasAdjacent = _adjacentState.HasAdjacent(direction);
            var text = hasAdjacent
                ? GetDirectionArrowText(direction)
                : "＋";

            using var backBrush = new SolidBrush(
                hasAdjacent
                    ? Color.FromArgb(90, 90, 110)
                    : Color.FromArgb(70, 100, 70)
            );

            using var borderPen = new Pen(Color.FromArgb(180, Color.White));
            using var textBrush = new SolidBrush(Color.White);

            g.FillEllipse(backBrush, rect);
            g.DrawEllipse(borderPen, rect);

            using var font = new Font("Yu Gothic UI", 10f, FontStyle.Bold);

            var format = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            };

            g.DrawString(text, font, textBrush, rect, format);
        }
    }

    private static string GetDirectionArrowText(PageDirection direction)
    {
        return direction switch
        {
            PageDirection.Up => "▲",
            PageDirection.Down => "▼",
            PageDirection.Left => "◀",
            PageDirection.Right => "▶",
            _ => string.Empty
        };
    }

    private Rectangle GetAdjacentNavigationButtonRect(PageDirection direction)
    {
        if (_tileMap == null)
            return Rectangle.Empty;

        var margin = ViewerConstants.MapViewMargin;
        var tileSize = CurrentTileRenderSize;

        var mapLeft = OffsetX;
        var mapTop = OffsetY;
        var mapWidth = _tileMap.Width * tileSize;
        var mapHeight = _tileMap.Height * tileSize;
        var mapRight = mapLeft + mapWidth;
        var mapBottom = mapTop + mapHeight;

        const int buttonSize = 24;

        return direction switch
        {
            // Up / Left は「番号帯より外側」の固定マージン帯（コントロール端基準）に配置する
            PageDirection.Up => new Rectangle(
                mapLeft + mapWidth / 2 - buttonSize / 2,
                (margin - buttonSize) / 2,
                buttonSize,
                buttonSize),

            PageDirection.Down => new Rectangle(
                mapLeft + mapWidth / 2 - buttonSize / 2,
                mapBottom + (margin - buttonSize) / 2,
                buttonSize,
                buttonSize),

            PageDirection.Left => new Rectangle(
                (margin - buttonSize) / 2,
                mapTop + mapHeight / 2 - buttonSize / 2,
                buttonSize,
                buttonSize),

            PageDirection.Right => new Rectangle(
                mapRight + (margin - buttonSize) / 2,
                mapTop + mapHeight / 2 - buttonSize / 2,
                buttonSize,
                buttonSize),

            _ => Rectangle.Empty
        };
    }

    private bool TryHitAdjacentNavigationButton(Point point, out PageDirection direction)
    {
        foreach (var dir in new[]
        {
            PageDirection.Up,
            PageDirection.Down,
            PageDirection.Left,
            PageDirection.Right
        })
        {
            if (GetAdjacentNavigationButtonRect(dir).Contains(point))
            {
                direction = dir;
                return true;
            }
        }

        direction = default;
        return false;
    }

    public void ZoomIn()
    {
        SetZoomScale(_zoomScale + ZoomStep);
    }

    public void ZoomOut()
    {
        SetZoomScale(_zoomScale - ZoomStep);
    }

    public void ResetZoom()
    {
        SetZoomScale(DefaultZoomScale);
    }

    public void SetZoomScale(float scale)
    {
        var clamped = Math.Clamp(scale, MinZoomScale, MaxZoomScale);

        if (Math.Abs(_zoomScale - clamped) < 0.001f)
            return;

        _zoomScale = clamped;

        UpdatePreferredControlSize();

        ZoomChanged?.Invoke(this, EventArgs.Empty);
        Invalidate();
    }

    public Size GetPreferredContentSize()
    {
        var width = (_tileMap?.Width ?? MapConstants.PageTileWidth) * CurrentTileRenderSize;
        var height = (_tileMap?.Height ?? MapConstants.PageTileHeight) * CurrentTileRenderSize;

        return new Size(
            width + OffsetX + ViewerConstants.MapViewMargin,
            height + OffsetY + ViewerConstants.MapViewMargin
        );
    }

    private void UpdatePreferredControlSize()
    {
        var size = GetPreferredContentSize();

        MinimumSize = size;

        if (Dock == DockStyle.None)
        {
            Size = size;
        }
    }

    /// <summary>
    /// タイル座標(x, y)に対応する画面上の描画矩形を返す。
    /// </summary>
    public Rectangle GetTileRect(int x, int y)
    {
        var dstSize = CurrentTileRenderSize;

        return new Rectangle(
            OffsetX + x * dstSize,
            OffsetY + y * dstSize,
            dstSize,
            dstSize);
    }

    private void DrawSearchHighlights(Graphics g)
    {
        // ハイライト表示ONの場合、ヒット全件を塗りつぶし表示
        if (_showSearchHighlight && _searchHighlights.Count > 0)
        {
            using var fillBrush = new SolidBrush(SearchVisualConstants.HighlightFillColor);

            foreach (var hit in _searchHighlights)
            {
                g.FillRectangle(fillBrush, GetTileRect(hit.X, hit.Y));
            }
        }

        // 現在のジャンプ先タイルには、ハイライトON/OFFに関わらず常に枠線を表示する
        // （ヒット全件と現在位置を視覚的に区別するため）
        if (_currentSearchHit is { } current)
        {
            using var pen = new Pen(
                SearchVisualConstants.HighlightBorderColor,
                SearchVisualConstants.HighlightBorderWidth);

            g.DrawRectangle(pen, GetTileRect(current.X, current.Y));
        }
    }

    private void DrawColumnNumbers(Graphics g)
    {
        if (!_showColumnNumbers || _tileMap == null) return;

        var dstSize = CurrentTileRenderSize;
        var offsetX = OffsetX;
        var bandTop = OffsetY - ViewerConstants.ColumnNumberBandHeight;

        using var font = new Font("Yu Gothic UI", 7f);
        using var brush = new SolidBrush(Color.Gainsboro);
        var format = new StringFormat
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center
        };

        for (var x = 0; x < _tileMap.Width; x++)
        {
            var rect = new Rectangle(
                offsetX + x * dstSize,
                bandTop,
                dstSize,
                ViewerConstants.ColumnNumberBandHeight);

            g.DrawString(x.ToString(), font, brush, rect, format);
        }
    }

    private void DrawRowNumbers(Graphics g)
    {
        if (!_showRowNumbers || _tileMap == null) return;

        var dstSize = CurrentTileRenderSize;
        var offsetY = OffsetY;
        var bandLeft = OffsetX - ViewerConstants.RowNumberBandWidth;

        using var font = new Font("Yu Gothic UI", 7f);
        using var brush = new SolidBrush(Color.Gainsboro);
        var format = new StringFormat
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center
        };

        for (var y = 0; y < _tileMap.Height; y++)
        {
            var rect = new Rectangle(
                bandLeft,
                offsetY + y * dstSize,
                ViewerConstants.RowNumberBandWidth,
                dstSize);

            g.DrawString(y.ToString(), font, brush, rect, format);
        }
    }
}

public sealed class TileContextMenuEventArgs : EventArgs
{
    public int TileX { get; }
    public int TileY { get; }
    public Point ScreenLocation { get; }

    public TileContextMenuEventArgs(int tileX, int tileY, Point screenLocation)
    {
        TileX = tileX;
        TileY = tileY;
        ScreenLocation = screenLocation;
    }
}

public sealed class MapAdjacentState
{
    public byte Up { get; init; } = 0xFF;
    public byte Down { get; init; } = 0xFF;
    public byte Left { get; init; } = 0xFF;
    public byte Right { get; init; } = 0xFF;

    public byte GetRoomId(PageDirection direction)
    {
        return direction switch
        {
            PageDirection.Up => Up,
            PageDirection.Down => Down,
            PageDirection.Left => Left,
            PageDirection.Right => Right,
            _ => 0xFF
        };
    }

    public bool HasAdjacent(PageDirection direction)
    {
        return GetRoomId(direction) != 0xFF;
    }
}

public sealed class AdjacentNavigationRequestedEventArgs : EventArgs
{
    public PageDirection Direction { get; }
    public bool HasAdjacentPage { get; }

    public AdjacentNavigationRequestedEventArgs(PageDirection direction, bool hasAdjacentPage)
    {
        Direction = direction;
        HasAdjacentPage = hasAdjacentPage;
    }
}
