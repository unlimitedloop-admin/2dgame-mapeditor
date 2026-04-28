using StageSmith.Core.Constants;

namespace StageSmith.Editor;

public partial class MainForm
{
    private void MainForm_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Control && e.Shift && e.KeyCode == Keys.S)
        {
            SaveProject();
            e.SuppressKeyPress = true;
        }
        else if (e.Control && e.KeyCode == Keys.Z)
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
        else if (e.Control && e.KeyCode == Keys.C)
        {
            CopySelection();
            e.SuppressKeyPress = true;
        }
        else if (e.Control && e.KeyCode == Keys.V)
        {
            if (_clipboard == null) return;
            var pos = _mapView.GetHoverTile();
            if (pos.X < 0 || pos.Y < 0) return;
            PasteSelection(pos.X, pos.Y);
            e.SuppressKeyPress = true;
        }
        else if (e.KeyCode == Keys.Escape)
        {
            _selectionTool?.ClearSelection();
            _mapView.Invalidate();
            e.SuppressKeyPress = true;
        }
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

    private void btnGrid_Click(object? sender, EventArgs e)
    {
        _showGrid = !_showGrid;
        _mapView.SetShowGrid(_showGrid);
    }

    private void btnTilePreview_Click(object sender, EventArgs e)
    {
        _mapView.ShowPreview = !_mapView.ShowPreview;
        UpdateTilePreviewIcon();
    }

    //private void btnPenTool_Click(object sender, EventArgs e)
    //{
    //    SetToolMode(EditorToolMode.Pen);
    //}

    //private void btnSelectionTool_Click(object sender, EventArgs e)
    //{
    //    SetToolMode(EditorToolMode.Selection);
    //}

    private void UpdateTilePreviewIcon()
    {
        if (_mapView.ShowPreview)
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
