using WeifenLuo.WinFormsUI.Docking;
using StageSmith.Editor.Controls;

namespace StageSmith.Editor.DockContents;

/// <summary>
/// マーカーの色設定パネルを格納する DockContent。
/// </summary>
public class MarkerColorPanelContent : DockContent
{
    public MarkerColorPanelControl MarkerColorPanel { get; }

    public MarkerColorPanelContent()
    {
        Text = "Marker Color";
        HideOnClose = true;
        CloseButtonVisible = true;
        DockAreas = DockAreas.DockLeft
                  | DockAreas.DockRight
                  | DockAreas.Float;

        MarkerColorPanel = new MarkerColorPanelControl
        {
            Dock = DockStyle.Fill
        };

        Controls.Add(MarkerColorPanel);
    }
}
