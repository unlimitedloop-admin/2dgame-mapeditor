using StageSmith.Core.Constants;
using StageSmith.Core.Models;
using StageSmith.Editor.Constants;
using StageSmith.Editor.Tools;
using StageSmith.Editor.Utilities;

namespace StageSmith.Editor.Controls;

// MapViewControl の描画専用partial（OnPaintとDraw*系）。
// 分割方針は MapViewControl.cs 参照。
public partial class MapViewControl
{
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

        DrawTileNumbers(g);

        DrawColumnNumbers(g);
        DrawRowNumbers(g);

        DrawSearchHighlights(g);
        DrawMarkers(g);
        DrawEntityLayer(g);
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

    private void DrawTileNumbers(Graphics g)
    {
        if (!_showTileNumbers || _tileMap == null) return;

        using var font = new Font("Yu Gothic UI", 7f);
        using var brush = new SolidBrush(Color.FromArgb(220, Color.White));
        using var shadowBrush = new SolidBrush(Color.FromArgb(220, Color.Black));

        var format = new StringFormat
        {
            Alignment = StringAlignment.Near,
            LineAlignment = StringAlignment.Near
        };

        for (var y = 0; y < _tileMap.Height; y++)
        {
            for (var x = 0; x < _tileMap.Width; x++)
            {
                var tileId = _tileMap.GetTile(x, y);
                var text = tileId.ToString();

                var rect = GetTileRect(x, y);
                var textPoint = new PointF(rect.X + 2, rect.Y + 1);
                var shadowPoint = new PointF(textPoint.X + 1, textPoint.Y + 1);

                g.DrawString(text, font, shadowBrush, shadowPoint, format);
                g.DrawString(text, font, brush, textPoint, format);
            }
        }
    }

    private void DrawEntityLayer(Graphics g)
    {
        if (!_showEntities || EntityLayer == null || _tileMap == null) return;

        // 部屋の端に置いたエンティティのスプライトが、番号帯や余白へはみ出さないようにマップ領域でクリップする
        var mapRect = new Rectangle(
            OffsetX,
            OffsetY,
            _tileMap.Width * CurrentTileRenderSize,
            _tileMap.Height * CurrentTileRenderSize);

        var state = g.Save();
        try
        {
            g.SetClip(mapRect);
            EntityLayer.Draw(g, OffsetX, OffsetY, RoomPixelScale);
        }
        finally
        {
            g.Restore(state);
        }
    }

    private void DrawPreview(Graphics g)
    {
        if (!_showPreview) return;

        // ピクセル座標ツール（オブジェクト配置）使用中はタイルのプレビューを出さない
        if (_toolManager?.CurrentTool is IPixelTool) return;
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

    private void DrawMarkers(Graphics g)
    {
        if (_tileMap == null) return;
        if (MarkerState == null || !MarkerState.ShowOverlay) return;

        using var brush = new SolidBrush(Color.FromArgb(128, _markerColor));

        foreach (var marker in MarkerState.GetMarkersForPage(_currentStageId, _currentPageIndex))
        {
            g.FillRectangle(brush, GetTileRect(marker.X, marker.Y));
        }

        if (MarkerState.CurrentMarker is { } current && current.StageId == _currentStageId && current.PageIndex == _currentPageIndex)
        {
            using var pen = new Pen(_markerColor, 2);
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

            g.DrawString(NumberFormatHelper.FormatColumnIndex(x, _numberDisplayFormat), font, brush, rect, format);
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

            g.DrawString(NumberFormatHelper.FormatRowIndex(y, _numberDisplayFormat), font, brush, rect, format);
        }
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
}
