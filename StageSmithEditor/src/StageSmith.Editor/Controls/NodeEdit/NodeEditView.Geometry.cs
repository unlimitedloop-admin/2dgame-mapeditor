using StageSmith.Core.Models;

namespace StageSmith.Editor.Controls;

// NodeEditView の座標変換・ヒットテスト専用partial（描画/入力の両方から使われる共通部分）。
// 分割方針は NodeEditView.cs 参照。
public partial class NodeEditView
{
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

    /// <summary>
    /// スクリーン座標のページインデックスを返す。なければ-1。
    /// </summary>
    /// <param name="screenPos">スクリーン座標。</param>
    /// <returns>ページインデックス。なければ-1。</returns>
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

    /// <summary>
    /// スクリーン座標が候補位置に当たるかチェックしグリッド座標を返す。なければnull。
    /// </summary>
    /// <param name="screenPos">スクリーン座標。</param>
    /// <returns>グリッド座標。なければnull。</returns>
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

    private static IEnumerable<Point> GetNeighborPositions(Point pos)
    {
        yield return new Point(pos.X - 1, pos.Y);
        yield return new Point(pos.X + 1, pos.Y);
        yield return new Point(pos.X,     pos.Y - 1);
        yield return new Point(pos.X,     pos.Y + 1);
    }
}
