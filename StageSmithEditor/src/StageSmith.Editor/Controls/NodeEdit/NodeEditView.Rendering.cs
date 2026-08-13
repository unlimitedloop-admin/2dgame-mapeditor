using StageSmith.Core.Models;
using StageSmith.Editor.Utilities;

namespace StageSmith.Editor.Controls;

// NodeEditView の描画専用partial（OnPaintとDraw*系）。
// 分割方針は NodeEditView.cs 参照。
public partial class NodeEditView
{
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

    /// <summary>
    /// 複製モード中にホバー位置をハイライト描画する。
    /// </summary>
    /// <param name="g">描画に使用するGraphicsオブジェクト。</param>
    /// <param name="stage">描画対象のステージ。</param>
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

    /// <summary>
    /// ノードD&D中にドロップ先グリッドにゴーストを描画する。
    /// </summary>
    /// <param name="g">描画に使用するGraphicsオブジェクト。</param>
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

        if (ShowPreview && _zoom >= PreviewZoomThreshold && _previewCache.Get(page.Id) is { } preview)
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
}
