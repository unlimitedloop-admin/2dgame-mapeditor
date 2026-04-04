namespace StageSmith.Editor;

public partial class MainForm
{
    private void btnUndo_Click(object sender, EventArgs e)
    {
        _commandManager.Undo();
        panel1.Invalidate();
    }

    private void btnRedo_Click(object sender, EventArgs e)
    {
        _commandManager.Redo();
        panel1.Invalidate();
    }
}
