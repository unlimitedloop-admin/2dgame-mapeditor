using StageSmith.Editor.Controls;
using WeifenLuo.WinFormsUI.Docking;

namespace StageSmith.Editor.DockContents;

/// <summary>
/// 敵などの画像オブジェクトを配置するためのパレット DockContent。
/// 初期状態では MetaTile Palette と同じペインにタブで並べる想定。
/// </summary>
public sealed class ObjectPaletteContent : DockContent
{
    public ObjectPaletteControl ObjectPalette { get; }

    public ObjectPaletteContent()
    {
        Text = "Object Palette";
        HideOnClose = true;
        CloseButtonVisible = true;
        DockAreas = DockAreas.DockLeft
                  | DockAreas.DockRight
                  | DockAreas.Float;

        ObjectPalette = new ObjectPaletteControl
        {
            Dock = DockStyle.Fill
        };

        Controls.Add(ObjectPalette);
    }
}
