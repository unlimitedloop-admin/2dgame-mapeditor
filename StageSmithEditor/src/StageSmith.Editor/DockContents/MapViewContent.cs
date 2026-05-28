using WeifenLuo.WinFormsUI.Docking;
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
            FixedPanel = FixedPanel.Panel2,    // タイルパレット側を固定高さに
            SplitterDistance = 240,
            Panel2MinSize = 128
        };

        MapView = new MapViewControl
        {
            Dock = DockStyle.Fill
        };

        TilePalette = new TilePaletteControl
        {
            Dock = DockStyle.Fill
        };

        splitContainer.Panel1.Controls.Add(MapView);
        splitContainer.Panel2.Controls.Add(TilePalette);

        Controls.Add(splitContainer);
    }
}
