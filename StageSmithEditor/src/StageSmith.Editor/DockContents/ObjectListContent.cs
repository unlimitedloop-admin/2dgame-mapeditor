using StageSmith.Editor.Controls;
using WeifenLuo.WinFormsUI.Docking;

namespace StageSmith.Editor.DockContents;

/// <summary>
/// 配置オブジェクトの検索・一覧パネルの DockContent。
/// 初期状態では Bookmark List と同じペインにタブで並べる想定。
/// </summary>
public sealed class ObjectListContent : DockContent
{
    public ObjectListControl ObjectList { get; }

    public ObjectListContent()
    {
        Text = "Object List";
        HideOnClose = true;
        CloseButtonVisible = true;
        DockAreas = DockAreas.DockLeft
                  | DockAreas.DockRight
                  | DockAreas.DockBottom
                  | DockAreas.Float;

        ObjectList = new ObjectListControl
        {
            Dock = DockStyle.Fill
        };

        Controls.Add(ObjectList);
    }
}
