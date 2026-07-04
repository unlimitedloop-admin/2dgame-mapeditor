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

    public void UpdateZoomTitle(float zoomScale)
    {
        Text = $"Map View ({zoomScale:0.0}x)";
    }
}
