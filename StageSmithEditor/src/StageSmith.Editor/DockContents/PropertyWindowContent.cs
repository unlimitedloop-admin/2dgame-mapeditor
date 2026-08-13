using WeifenLuo.WinFormsUI.Docking;
using StageSmith.Editor.Controls;

namespace StageSmith.Editor.DockContents;

/// <summary>
/// プロパティウィンドウを格納する DockContent。
/// デフォルトで右側にドッキングする。
/// </summary>
public class PropertyWindowContent : DockContent
{
    public PropertyWindowControl PropertyWindow { get; }

    public PropertyWindowContent()
    {
        Text = "Properties";
        HideOnClose = true;
        CloseButtonVisible = true;
        DockAreas = DockAreas.DockLeft
                  | DockAreas.DockRight
                  | DockAreas.Float;

        PropertyWindow = new PropertyWindowControl
        {
            Dock = DockStyle.Fill
        };

        Controls.Add(PropertyWindow);
    }
}
