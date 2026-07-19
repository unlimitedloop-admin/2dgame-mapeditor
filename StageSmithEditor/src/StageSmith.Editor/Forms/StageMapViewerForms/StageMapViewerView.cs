using StageSmith.Core.Constants;
using StageSmith.Core.Models;
using System.ComponentModel;

namespace StageSmith.Editor;

/// <summary>
/// ステージマップビューアーの中央描画領域。
/// 実際のゲーム画面と同じ品質でページを連結表示し、ハンドツールで自由にスクロールできる。
/// リアルタイム更新はせず、RefreshFromStage() 呼び出し時にのみ再描画する。
/// </summary>
public class StageMapViewerView : Panel
{
    //========================
    // ビュー状態
    //========================
    private PointF _viewOffset = PointF.Empty;
    private float _zoom = 1.0f;
    private const float ZoomMin = 0.25f;
    private const float ZoomMax = 4.0f;
    private const float ZoomStep = 0.25f;

    //========================
    // ページ配置単位（エディタと同じ2倍スケール基準）
    //========================
    private const int PageW = MapConstants.PageTileWidth * ViewerConstants.TileRenderSize;
    private const int PageH = MapConstants.PageTileHeight * ViewerConstants.TileRenderSize;
    private const int PageGap = 8;

    //========================
    // データ（RefreshFromStageで都度差し替え）
    //========================
    private List<Page> _pages = [];
    private readonly Dictionary<Guid, Bitmap> _pageCache = [];


    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int FilterZ { get; set; } = 0;

    //========================
    // イベント
    //========================
    public event Action<PointF>? ViewOffsetChanged;
    public event Action<float>? ZoomChanged;

    /// <summary>ビュー中心のグリッド座標が変わったとき発火する（ステータスバー表示用）。</summary>
    public event Action<int, int>? CenterGridChanged;

    //========================
    // ドラッグ
    //========================
    private Point _mouseDownPos;
    private bool _isDragging;

    public StageMapViewerView()
    {
        DoubleBuffered = true;
        BackColor = Color.FromArgb(20, 20, 24);

        MouseDown += OnMouseDown;
        MouseMove += OnMouseMove;
        MouseUp += OnMouseUp;
        MouseWheel += OnMouseWheel;
        Resize += (_, _) => NotifyCenterChanged();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            foreach (var bmp in _pageCache.Values)
                bmp.Dispose();
            _pageCache.Clear();
        }
        base.Dispose(disposing);
    }

    //========================
    // 更新（「更新」ボタンから呼ばれる唯一のデータ入口）
    //========================
    public void RefreshFromStage(Stage? stage, Bitmap? tileset)
    {
        foreach (var bmp in _pageCache.Values)
            bmp.Dispose();
        _pageCache.Clear();

        _pages = stage?.Pages.ToList() ?? [];

        if (tileset != null)
        {
            foreach (var page in _pages)
                _pageCache[page.Id] = BuildPageBitmap(page, tileset);
        }

        Invalidate();
    }

    /// <summary>
    /// 1ページ分を実解像度（2倍スケール）で描画したBitmapを生成する。
    /// MapViewControl / TilePaletteControlと同じ描画方針（NearestNeighbor）。
    /// </summary>
    private static Bitmap BuildPageBitmap(Page page, Bitmap tileset)
    {
        var tileMap = page.TileMap;
        var bmp = new Bitmap(tileMap.Width * ViewerConstants.TileRenderSize,
                              tileMap.Height * ViewerConstants.TileRenderSize);

        using var g = Graphics.FromImage(bmp);
        g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
        g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;
        g.Clear(Color.Black);

        const int srcSize = MapConstants.DefaultTileSize;
        var dstSize = ViewerConstants.TileRenderSize;
        var tilesPerRow = tileset.Width / srcSize;

        for (var y = 0; y < tileMap.Height; y++)
        {
            for (var x = 0; x < tileMap.Width; x++)
            {
                var tileId = tileMap.GetTile(x, y);

                var srcX = (tileId % tilesPerRow) * srcSize;
                var srcY = (tileId / tilesPerRow) * srcSize;
                var src = new Rectangle(srcX, srcY, srcSize, srcSize);
                var dst = new Rectangle(x * dstSize, y * dstSize, dstSize, dstSize);

                g.DrawImage(tileset, dst, src, GraphicsUnit.Pixel);
            }
        }

        return bmp;
    }

    //========================
    // 座標変換（NodeEditViewと同じ考え方、単位だけページサイズ）
    //========================
    private RectangleF PageToRect(Page page) => GridToRect(new Point(page.NodeX, page.NodeY));

    private RectangleF GridToRect(Point gridPos)
    {
        var stepX = (PageW + PageGap) * _zoom;
        var stepY = (PageH + PageGap) * _zoom;
        var x = _viewOffset.X + gridPos.X * stepX;
        var y = _viewOffset.Y + gridPos.Y * stepY;
        return new RectangleF(x, y, PageW * _zoom, PageH * _zoom);
    }

    private Point ScreenToGrid(Point screenPos)
    {
        var stepX = (PageW + PageGap) * _zoom;
        var stepY = (PageH + PageGap) * _zoom;
        var gx = (int)Math.Floor((screenPos.X - _viewOffset.X) / stepX);
        var gy = (int)Math.Floor((screenPos.Y - _viewOffset.Y) / stepY);
        return new Point(gx, gy);
    }

    //========================
    // 中心指定ワープ（ページ指定ジャンプ用）
    //========================
    public void CenterOnPageIndex(int pageIndex)
    {
        var page = _pages.ElementAtOrDefault(pageIndex);
        if (page == null) return;

        var stepX = (PageW + PageGap) * _zoom;
        var stepY = (PageH + PageGap) * _zoom;

        _viewOffset.X = ClientSize.Width / 2f - (page.NodeX * stepX + PageW * _zoom / 2f);
        _viewOffset.Y = ClientSize.Height / 2f - (page.NodeY * stepY + PageH * _zoom / 2f);

        ViewOffsetChanged?.Invoke(_viewOffset);
        NotifyCenterChanged();
        Invalidate();
    }

    private void NotifyCenterChanged()
    {
        var center = ScreenToGrid(new Point(ClientSize.Width / 2, ClientSize.Height / 2));
        CenterGridChanged?.Invoke(center.X, center.Y);
    }

    //========================
    // マウス操作（クリック判定不要、常にハンドツール）
    //========================
    private void OnMouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left) return;
        _mouseDownPos = e.Location;
        _isDragging = true;
    }

    private void OnMouseMove(object? sender, MouseEventArgs e)
    {
        if (!_isDragging || e.Button != MouseButtons.Left) return;

        var dx = e.X - _mouseDownPos.X;
        var dy = e.Y - _mouseDownPos.Y;

        _viewOffset.X += dx;
        _viewOffset.Y += dy;
        _mouseDownPos = e.Location;

        ViewOffsetChanged?.Invoke(_viewOffset);
        NotifyCenterChanged();
        Invalidate();
    }

    private void OnMouseUp(object? sender, MouseEventArgs e)
    {
        _isDragging = false;
    }

    private void OnMouseWheel(object? sender, MouseEventArgs e)
    {
        if (ModifierKeys.HasFlag(Keys.Control))
        {
            var delta = e.Delta > 0 ? ZoomStep : -ZoomStep;
            _zoom = Math.Clamp(_zoom + delta, ZoomMin, ZoomMax);
            ZoomChanged?.Invoke(_zoom);
            NotifyCenterChanged();
            Invalidate();
            return;
        }

        _viewOffset.Y += e.Delta > 0 ? 40 : -40;
        ViewOffsetChanged?.Invoke(_viewOffset);
        NotifyCenterChanged();
        Invalidate();
    }

    //========================
    // 描画
    //========================
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        var g = e.Graphics;
        g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;

        var viewport = ClientRectangle;

        foreach (var page in _pages)
        {
            if (page.Header.Z != FilterZ) continue;

            var rect = PageToRect(page);
            var screenRect = Rectangle.Round(rect);

            if (!screenRect.IntersectsWith(viewport)) continue;

            if (_pageCache.TryGetValue(page.Id, out var bmp))
            {
                g.DrawImage(bmp, screenRect);
            }
            else
            {
                // タイルセット未設定などでキャッシュが無い場合は枠だけ
                using var pen = new Pen(Color.FromArgb(80, 80, 90), 1);
                g.DrawRectangle(pen, screenRect);
            }
        }

        DrawEntityOverlay(g);
    }

    /// <summary>
    /// 将来、敵・アイテム・トラップギミックなどを重ね描画するためのフック。
    /// 現時点では未実装（アニメーションなしの静的オーバーレイを想定）。
    /// </summary>
    protected virtual void DrawEntityOverlay(Graphics g)
    {
        // TODO: 将来のエンティティ表示機能で実装
    }
}
