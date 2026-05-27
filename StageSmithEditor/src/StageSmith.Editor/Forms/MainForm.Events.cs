using StageSmith.Core.Constants;

namespace StageSmith.Editor;

public partial class MainForm
{
    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        // ── Ctrl 系（TextBox フォーカス中でも有効） ──────────────────
        switch (keyData)
        {
            case Keys.Control | Keys.Shift | Keys.S:
                SaveProject();
                return true;

            case Keys.Control | Keys.Z:
                _commandManager.Undo();
                _mapView.Invalidate();
                return true;

            case Keys.Control | Keys.Y:
                _commandManager.Redo();
                _mapView.Invalidate();
                return true;

            case Keys.Control | Keys.O:
                OpenProject();
                return true;

            case Keys.Control | Keys.N:
                NewProject();
                return true;

            case Keys.Control | Keys.C:
                CopySelection();
                return true;

            case Keys.Control | Keys.V:
                if (_clipboard == null) return true;
                var pos = _mapView.GetHoverTile();
                if (pos.X < 0 || pos.Y < 0) return true;
                PasteSelection(pos.X, pos.Y);
                return true;
        }

        // ── TextBox フォーカス中の Esc でフォーカスアウト ────────────
        if (keyData == Keys.Escape && IsFocusedOnTextBox())
        {
            _mapView.Focus();
            return true;
        }

        // ── TextBox フォーカス中はここで終了 ─────────────────────────
        if (IsFocusedOnTextBox())
            return base.ProcessCmdKey(ref msg, keyData);

        // ── 単体キー（TextBox 以外のとき有効） ───────────────────────
        switch (keyData)
        {
            case Keys.Escape:
                CancelDrag();
                _selectionTool?.ClearSelection();
                _mapView.Invalidate();
                return true;

            case Keys.P:
                SetToolMode(EditorToolMode.Pen);
                return true;

            case Keys.S:
                SetToolMode(EditorToolMode.Selection);
                return true;

            case Keys.Insert:
                ApplySelectionFill();
                return true;

            case Keys.Enter:
                _selectionTool?.OnConfirm();
                return true;

            case Keys.Delete:
                DeleteSelection();
                return true;

            case Keys.Control | Keys.D1:
            case Keys.Control | Keys.NumPad1:
                SetToolMode(EditorToolMode.Pen);
                return true;

            case Keys.Control | Keys.D2:
            case Keys.Control | Keys.NumPad2:
                SetToolMode(EditorToolMode.Selection);
                return true;
        }

        return base.ProcessCmdKey(ref msg, keyData);
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

    private void CancelDrag()
    {
        _currentDragCommand = null;
        _mapView.Invalidate();
    }

    private static bool IsFocusedOnTextBox()
    {
        var focused = GetFocusedControl(Form.ActiveForm);
        return focused is TextBox;
    }

    private static Control? GetFocusedControl(Control? parent)
    {
        if (parent == null) return null;

        // ContainerControl（UserControl含む）は ActiveControl を持つ
        if (parent is ContainerControl container && container.ActiveControl != null)
            return GetFocusedControl(container.ActiveControl);

        return parent;
    }
}
