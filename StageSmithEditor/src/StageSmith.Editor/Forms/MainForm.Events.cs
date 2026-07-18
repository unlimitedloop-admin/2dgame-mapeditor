using StageSmith.Core.Constants;
using StageSmith.Editor.Controls;

namespace StageSmith.Editor;

public partial class MainForm
{
    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        // ── Ctrl / Alt 系（TextBox フォーカス中でも有効） ──────────────────
        switch (keyData)
        {
            case Keys.Control | Keys.Shift | Keys.S:
                SaveProject();
                return true;

            case Keys.Control | Keys.Z:
                _commandManager.Undo();
                return true;

            case Keys.Control | Keys.Y:
                _commandManager.Redo();
                return true;

            case Keys.Control | Keys.B:
                if (_context.CurrentStage is { } stage && _context.CurrentPage is { } page)
                    ToggleBookmark(stage, page);
                return true;

            // 隣接Room移動
            case Keys.Control | Keys.Left:
                NavigateAdjacentRoom(Direction.Left);
                return true;

            case Keys.Control | Keys.Right:
                NavigateAdjacentRoom(Direction.Right);
                return true;

            case Keys.Control | Keys.Up:
                NavigateAdjacentRoom(Direction.Up);
                return true;

            case Keys.Control | Keys.Down:
                NavigateAdjacentRoom(Direction.Down);
                return true;

            // Zレイヤー移動
            case Keys.Alt | Keys.Left:
                NavigateBack();
                return true;

            case Keys.Alt | Keys.Right:
                NavigateForward();
                return true;

            case Keys.Control | Keys.T:
                AddPageToCurrentStage();
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

            case Keys.Shift | Keys.Escape:
                ClearSearchHighlight();
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

            case Keys.PageDown:
                NavigatePage(NavAction.Next);
                return true;

            case Keys.PageUp:
                NavigatePage(NavAction.Prev);
                return true;

           case Keys.P:
                SetToolMode(EditorToolMode.Pen);
                return true;

            case Keys.S:
                SetToolMode(EditorToolMode.Selection);
                return true;

            case Keys.G:
                ShowMapViewGrid();
                _menuViewGridLines.Checked = _showGrid;
                return true;

            case Keys.T:
                _mapView.ShowPreview = !_mapView.ShowPreview;
                UpdateTilePreviewIcon();
                return true;

            case Keys.F:
                FillSelection();
                return true;

            case Keys.M:
                _menuViewMarkerOverlay.Checked = !_menuViewMarkerOverlay.Checked;
                ToggleMarkerOverlay(_menuViewMarkerOverlay.Checked);
                return true;

            case Keys.L:
                _menuViewShowTileNumbers.Checked = !_menuViewShowTileNumbers.Checked;
                ToggleShowTileNumbers(_menuViewShowTileNumbers.Checked);
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

    private void UpdateTilePreviewIcon()
    {
        if (_mapView.ShowPreview)
        {
            _tilePreviewButton.Image = StageSmithEditor.Properties.Resources.icons8_目に見える_24;
        }
        else
        {
            _tilePreviewButton.Image = StageSmithEditor.Properties.Resources.icons8_目に見えない_24;
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
