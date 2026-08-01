using StageSmith.Editor.Forms;

namespace StageSmith.Editor;

public partial class MainForm
{
    private void OpenEditorPropertiesDialog()
    {
        using var dialog = new EditorPropertiesDialog(_config);

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            _config = dialog.Result;

            ApplyNumberDisplayFormat();
        }
    }
}
