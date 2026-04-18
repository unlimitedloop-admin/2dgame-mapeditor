namespace StageSmith.Editor;

public partial class MainForm
{
    private void MainForm_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Control && e.KeyCode == Keys.Z)
        {
            _commandManager.Undo();
            _mapView.Invalidate();
            e.SuppressKeyPress = true;
        }
        else if (e.Control && e.KeyCode == Keys.Y)
        {
            _commandManager.Redo();
            _mapView.Invalidate();
            e.SuppressKeyPress = true;
        }
        else if (e.Control && e.Shift && e.KeyCode == Keys.S)
        {
            btnSave_Click(sender!, e);
            e.SuppressKeyPress = true;
        }
    }

    private void btnGrid_Click(object? sender, EventArgs e)
    {
        _showGrid = !_showGrid;
        _mapView.SetShowGrid(_showGrid);
    }

    private void btnTilePreview_Click(object sender, EventArgs e)
    {
        _showPreview = !_showPreview;
        _mapView.ShowPreview = _showPreview;
        UpdateTilePreviewIcon();
    }

    private void UpdateTilePreviewIcon()
    {
        if (_showPreview)
        {
            btnTilePreview.Image = StageSmithEditor.Properties.Resources.icons8_目に見える_24;
        }
        else
        {
            btnTilePreview.Image = StageSmithEditor.Properties.Resources.icons8_目に見えない_24;
        }
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (keyData == Keys.Escape)
        {
            CancelDrag();
            _mapView.ClearSelection();
            return true;
        }

        return base.ProcessCmdKey(ref msg, keyData);
    }

    private void CancelDrag()
    {
        _currentDragCommand = null;
        _mapView.Invalidate();
    }
}
