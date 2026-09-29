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

            case Keys.Control | Keys.M:
                MoveToNextMarker();
                return true;

            case Keys.Control | Keys.Shift | Keys.M:
                MoveToPreviousMarker();
                return true;

            case Keys.Control | Keys.J:
                OpenJumpPageDialog();
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

            case Keys.Control | Keys.P:
                NewProject();
                return true;

            case Keys.Control | Keys.N:
                NewStage();
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
        {
            // Copy/Paste/Cut/Delete は Edit メニューにも同じショートカットが
            // 割り当てられているため、base.ProcessCmdKey へそのまま渡すと
            // メニュー側(ToolStripMenuItem.ShortcutKeys)に横取りされ、
            // TextBox標準のクリップボード操作まで届かない。
            // この4キーだけは base を経由させず、そのままコントロールへ渡す
            // （マウスでのメニュークリックには影響しない）。
            switch (keyData)
            {
                case Keys.Control | Keys.C:
                case Keys.Control | Keys.V:
                case Keys.Control | Keys.X:
                case Keys.Delete:
                    return false;
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        // ── Object List にフォーカスがある間は一覧の操作を優先する ─────────────
        // Delete＝一覧で選んだものを削除、Enter・矢印・Home/End・PageUp/Down（Shift併用の範囲選択含む）は
        // 一覧自身に渡す（ここで横取りすると、ジャンプ・行移動の代わりにマップ側の操作が動いてしまう）。
        if (_objectListContent.ContainsFocus)
        {
            if (keyData == Keys.Delete)
            {
                _objectList.DeleteSelected();
                return true;
            }

            var modifiers = keyData & Keys.Modifiers;
            if ((modifiers == Keys.None || modifiers == Keys.Shift) &&
                (keyData & Keys.KeyCode) is Keys.Enter or Keys.Up or Keys.Down or Keys.Home or Keys.End or Keys.PageUp or Keys.PageDown)
            {
                return false;
            }
        }

        // ── Objectツールで選択中のエンティティを矢印キーで微調整（Shift で 8px） ──
        if (_currentMode == EditorToolMode.Object && TryNudgeSelectedEntity(keyData))
            return true;

        // ── 単体キー（TextBox 以外のとき有効） ───────────────────────
        switch (keyData)
        {
            case Keys.Escape:
                CancelDrag();
                _selectionTool?.ClearSelection();
                _objectTool?.ClearSelection();
                _objectPalette.SetPlayerStartMode(false);
                _mapView.Invalidate();
                return true;

            case Keys.Control | Keys.A:
                SelectAllTiles();
                return true;

            case Keys.Control | Keys.C:
                CopySelection();
                return true;

            case Keys.Control | Keys.V:
                PasteSelection();
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

            case Keys.K:
                SetToolMode(EditorToolMode.Marker);
                return true;

            case Keys.O:
                SetToolMode(EditorToolMode.Object);
                return true;

            case Keys.E:
                _menuViewShowEntities.Checked = !_menuViewShowEntities.Checked;
                return true;

            case Keys.B:
                SetToolMode(EditorToolMode.Bucket);
                return true;

            case Keys.G:
                _menuViewGridLines.Checked = !_menuViewGridLines.Checked;
                return true;

            case Keys.T:
                _menuViewTilePreview.Checked = !_menuViewTilePreview.Checked;
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

            case Keys.R:
                _menuViewRowNumbers.Checked = !_menuViewRowNumbers.Checked;
                return true;

            case Keys.C:
                _menuViewColumnNumbers.Checked = !_menuViewColumnNumbers.Checked;
                return true;

            case Keys.I:
                _menuViewTileInfo.Checked = !_menuViewTileInfo.Checked;
                return true;

            case Keys.Insert:
                ApplySelectionFill();
                return true;

            case Keys.Enter:
                _selectionTool?.OnConfirm();
                return true;

            case Keys.Delete:
                if (_currentMode == EditorToolMode.Object)
                    _objectTool?.DeleteSelected();
                else
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

            case Keys.Control | Keys.D3:
            case Keys.Control | Keys.NumPad3:
                SetToolMode(EditorToolMode.Bucket);
                return true;

            case Keys.Control | Keys.D4:
            case Keys.Control | Keys.NumPad4:
                SetToolMode(EditorToolMode.Marker);
                return true;

            case Keys.Control | Keys.D5:
            case Keys.Control | Keys.NumPad5:
                SetToolMode(EditorToolMode.Object);
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

    private void ApplyTilePreviewState(bool visible)
    {
        _mapView.ShowPreview = visible;
        _tilePreviewButton.Checked = visible;
        UpdateTilePreviewIcon();
    }

    private void ApplyGridState(bool visible)
    {
        _showGrid = visible;
        _mapView.SetShowGrid(_showGrid);
        _showGridButton.Checked = visible;
    }

    private void ToggleShowTileNumbers(bool show)
    {
        _mapView.SetShowTileNumbers(show);
        _numberLabelButton.Checked = show;
    }

    private void ApplyRowNumberState(bool show)
    {
        _mapView.SetShowRowNumbers(show);
    }

    private void ApplyColumnNumberState(bool show)
    {
        _mapView.SetShowColumnNumbers(show);
    }

    private void ToggleMarkerOverlay(bool show)
    {
        _markerState.ShowOverlay = show;
        _mapView.Invalidate();
    }

    private void CancelDrag()
    {
        _currentDragCommand = null;
        _mapView.Invalidate();
    }

    private bool IsFocusedOnTextBox()
    {
        var focused = GetFocusedControl(Form.ActiveForm);
        return focused is TextBox || _stageExplorer.IsEditingLabel;
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
