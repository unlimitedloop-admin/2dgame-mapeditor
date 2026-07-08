using WeifenLuo.WinFormsUI.Docking;
using StageSmith.Core.Constants;
using StageSmith.Editor.Controls;

namespace StageSmith.Editor.DockContents;

/// <summary>
/// マップビューとタイルパレットを格納する DockContent。
/// 中央固定パネルとして使用する。
/// </summary>
public class MapViewContent : DockContent
{
    private readonly Panel _mapScrollPanel;

    public MapViewControl MapView { get; }
    public TilePaletteControl TilePalette { get; }
    public PageNavBarControl PageNavBar { get; }

    public MapViewContent()
    {
        Text = "Map View (1.0x)";
        HideOnClose = true;
        CloseButtonVisible = true;
        DockAreas = DockAreas.Document;

        // 上下に分割（マップビュー上、タイルパレット下）
        var splitContainer = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Horizontal,
            FixedPanel = FixedPanel.Panel1,
            Panel1MinSize = ViewerConstants.MapViewRenderSize.Height + 20,
            Panel2MinSize = ViewerConstants.TileRenderSize * 2
        };

        // レイアウト確定後に SplitterDistance を設定する
        splitContainer.Layout += (s, e) =>
        {
            var sc = (SplitContainer)s!;
            var target = ViewerConstants.MapViewContentSize.Height
                       + ViewerConstants.NavBarHeight
                       + ViewerConstants.MapViewPanelExtraHeight;
            if (sc.SplitterDistance != target && sc.Height > target)
            {
                sc.SplitterDistance = target;
            }
        };

        // MapView + PageNavBar を Panel1 に収める
        _mapScrollPanel = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            BackColor = SystemColors.ControlDark
        };

        MapView = new MapViewControl
        {
            Dock = DockStyle.None,
            Location = new Point(0, 0)
        };

        MapView.Size = MapView.GetPreferredContentSize();

        MapView.ZoomChanged += (_, _) =>
        {
            MapView.Size = MapView.GetPreferredContentSize();
            _mapScrollPanel.AutoScrollMinSize = MapView.Size;
        };

        _mapScrollPanel.Controls.Add(MapView);
        _mapScrollPanel.AutoScrollMinSize = MapView.Size;

        PageNavBar = new PageNavBarControl
        {
            Dock = DockStyle.Bottom
        };

        splitContainer.Panel1.Controls.Add(_mapScrollPanel);
        splitContainer.Panel1.Controls.Add(PageNavBar);

        TilePalette = new TilePaletteControl
        {
            Dock = DockStyle.Fill
        };

        splitContainer.Panel2.Controls.Add(TilePalette);

        Controls.Add(splitContainer);
    }

    /// <summary>
    /// 指定したタイル座標がスクロール表示範囲外の場合、表示範囲内（中央）へスクロールする。
    /// すでに表示範囲内であれば何もしない。
    /// </summary>
    public void ScrollToTile(int x, int y)
    {
        var localRect = MapView.GetTileRect(x, y);

        // MapView はスクロールパネル内の子コントロールなので、
        // その Location を加算してパネル内座標に変換する。
        var target = new Rectangle(
            MapView.Location.X + localRect.X,
            MapView.Location.Y + localRect.Y,
            localRect.Width,
            localRect.Height);

        ScrollRectIntoView(target);
    }

    private void ScrollRectIntoView(Rectangle target)
    {
        var scrollPos = _mapScrollPanel.AutoScrollPosition;

        var viewport = new Rectangle(
            -scrollPos.X,
            -scrollPos.Y,
            _mapScrollPanel.ClientSize.Width,
            _mapScrollPanel.ClientSize.Height);

        if (viewport.Contains(target))
            return; // 既に見えているので何もしない

        // 対象タイルがパネル中央に来るようスクロール位置を算出する
        var newX = target.X - (_mapScrollPanel.ClientSize.Width - target.Width) / 2;
        var newY = target.Y - (_mapScrollPanel.ClientSize.Height - target.Height) / 2;

        newX = Math.Max(0, newX);
        newY = Math.Max(0, newY);

        _mapScrollPanel.AutoScrollPosition = new Point(newX, newY);
    }

    public void UpdateZoomTitle(float zoomScale)
    {
        Text = $"Map View ({zoomScale:0.0}x)";
    }
}
