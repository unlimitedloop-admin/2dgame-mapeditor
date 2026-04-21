using StageSmith.Core.Constants;

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
        else if (e.Control && e.KeyCode == Keys.C)
        {
            CopySelection();
            e.SuppressKeyPress = true;
        }
        else if (e.Control && e.KeyCode == Keys.V)
        {
            var pos = _mapView.GetHoverTile();
            if (pos.X >= 0 && pos.Y >= 0)
            {
                PasteSelection(pos.X, pos.Y);
                e.SuppressKeyPress = true;
            }
        }
        // PREVIEW: Show preview while Control is held down, but only if there's something in the clipboard.
        // The clipboard should be cleared the moment the selection is deselected, but the preview keeps appearing without taking this into account.
        //else if (e.KeyCode == Keys.ControlKey)
        //{
        //    _mapView.ShowPreview = _clipboard != null;
        //}
        else if (e.KeyCode == Keys.P)
        {
            SetToolMode(EditorToolMode.Pen);
            e.SuppressKeyPress = true;
        }
        else if (e.KeyCode == Keys.S)
        {
            SetToolMode(EditorToolMode.Selection);
            e.SuppressKeyPress = true;
        }
        else if (e.KeyCode == Keys.Insert)
        {
            ApplySelectionFill();
            e.SuppressKeyPress = true;
        }
        else if (e.KeyCode == Keys.Delete)
        {
            DeleteSelection();
            e.SuppressKeyPress = true;
        }
    }

    private void MainForm_KeyUp(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.ControlKey)
        {
            _mapView.ShowPreview = false;
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

    private void btnPenTool_Click(object sender, EventArgs e)
    {
        SetToolMode(EditorToolMode.Pen);
    }

    private void btnSelectionTool_Click(object sender, EventArgs e)
    {
        SetToolMode(EditorToolMode.Selection);
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
            ClearSelection();
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
