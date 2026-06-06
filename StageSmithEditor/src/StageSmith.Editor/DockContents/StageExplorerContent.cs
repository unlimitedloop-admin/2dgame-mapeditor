using WeifenLuo.WinFormsUI.Docking;
using StageSmith.Editor.Controls;

namespace StageSmith.Editor.DockContents;

/// <summary>
/// ステージエクスプローラーを格納する DockContent。
/// デフォルトで左側にドッキングする。
/// </summary>
public class StageExplorerContent : DockContent
{
    public StageExplorerControl StageExplorer { get; }

    public StageExplorerContent()
    {
        Text = "Stage Explorer";
        CloseButtonVisible = true;
        DockAreas = DockAreas.DockLeft
                  | DockAreas.DockRight
                  | DockAreas.Float;

        StageExplorer = new StageExplorerControl
        {
            Dock = DockStyle.Fill
        };

        Controls.Add(StageExplorer);
    }
}
