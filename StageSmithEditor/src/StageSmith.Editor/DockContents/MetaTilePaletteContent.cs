using StageSmith.Editor.Controls;
using WeifenLuo.WinFormsUI.Docking;

namespace StageSmith.Editor.DockContents;

/// <summary>
/// 保存済みメタタイルを選択するための DockContent。
/// 初期状態では Stage Explorer の下へドッキングする想定。
/// </summary>
public sealed class MetaTilePaletteContent : DockContent
{
    public MetaTilePaletteControl MetaTilePalette { get; }

    public MetaTilePaletteContent()
    {
        Text = "MetaTile Palette";
        CloseButtonVisible = true;
        DockAreas = DockAreas.DockLeft
                  | DockAreas.DockRight
                  | DockAreas.Float;

        MetaTilePalette = new MetaTilePaletteControl
        {
            Dock = DockStyle.Fill
        };

        Controls.Add(MetaTilePalette);
    }
}
