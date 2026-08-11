using StageSmith.Core.Constants;
using StageSmith.Core.Models;

namespace StageSmith.Editor.Controls;

// MapViewControl の隣接ページナビゲーションボタン専用partial。
// 分割方針は MapViewControl.cs 参照。
public partial class MapViewControl
{
    // ===== Adjacent Navigation =====
    public event EventHandler<AdjacentNavigationRequestedEventArgs>? AdjacentNavigationRequested;

    private MapAdjacentState _adjacentState = new();

    public void SetAdjacentState(MapAdjacentState state)
    {
        _adjacentState = state;
        Invalidate();
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
}
