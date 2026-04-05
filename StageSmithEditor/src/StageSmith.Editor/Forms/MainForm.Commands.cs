namespace StageSmith.Editor;

public partial class MainForm
{
    private void btnUndo_Click(object sender, EventArgs e)
    {
        _commandManager.Undo();
        _mapView.Invalidate();
    }

    private void btnRedo_Click(object sender, EventArgs e)
    {
        _commandManager.Redo();
        _mapView.Invalidate();
    }
}
