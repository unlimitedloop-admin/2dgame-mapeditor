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
    public MapViewControl MapView { get; }
    public TilePaletteControl TilePalette { get; }
    public PageNavBarControl PageNavBar { get; }

    public MapViewContent()
    {
        Text = "Map View";
        CloseButtonVisible = false;     // 中央パネルは閉じさせない
        DockAreas = DockAreas.Document; // 中央ドキュメント領域に固定

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
                       + ViewerConstants.NavBarHeight;
            if (sc.SplitterDistance != target && sc.Height > target)
            {
                sc.SplitterDistance = target;
            }
        };

        // MapView + PageNavBar を Panel1 に収める
        MapView = new MapViewControl
        {
            Dock = DockStyle.Fill
        };

        PageNavBar = new PageNavBarControl();  // Dock.Bottom なので自動的に下に配置

        splitContainer.Panel1.Controls.Add(MapView);
        splitContainer.Panel1.Controls.Add(PageNavBar);

        TilePalette = new TilePaletteControl
        {
            Dock = DockStyle.Fill
        };

        splitContainer.Panel2.Controls.Add(TilePalette);

        Controls.Add(splitContainer);
    }
}
