namespace StageSmith.Editor.Controls;

// NodeEditView の入力処理専用partial（マウス操作）。
// 分割方針は NodeEditView.cs 参照。
public partial class NodeEditView
{
    //========================
    // マウス操作
    //========================
    private void OnMouseDown(object? sender, MouseEventArgs e)
    {
        _mouseDownPos = e.Location;
        _isDragging      = false;
        _isDraggingNode  = false;
        _isDraggingNodeCopy = false;
        _dragSourcePageIndex = -1;

        if (e.Button == MouseButtons.Right)
        {
            _contextMenuTargetPageIndex = HitTestPageIndex(e.Location);
            _contextMenuTargetCandidate = _contextMenuTargetPageIndex < 0
                ? HitTestCandidate(e.Location)
                : null;
        }
        else if (e.Button == MouseButtons.Left && ModifierKeys.HasFlag(Keys.Control))
        {
            // Ctrl押下中 → ノードD&D候補
            _dragSourcePageIndex = HitTestPageIndex(e.Location);
        }
    }

    private void OnMouseMove(object? sender, MouseEventArgs e)
    {
        // ホバー座標更新
        var grid = ScreenToGrid(e.Location);
        if (grid != _hoveredGridPos)
        {
            _hoveredGridPos = grid;
            CoordChanged?.Invoke(grid.X, grid.Y);
        }

        // 複製モード中はホバー位置を更新して再描画
        if (_isPasteMode)
        {
            var hoverPage      = HitTestPageIndex(e.Location);
            var hoverCandidate = hoverPage < 0 ? HitTestCandidate(e.Location) : null;

            Point? newHover;
            if (hoverPage >= 0)
            {
                var stage = _context.CurrentStage;
                var page  = stage?.Pages.ElementAtOrDefault(hoverPage);
                newHover  = page != null ? new Point(page.NodeX, page.NodeY) : null;
            }
            else if (hoverCandidate.HasValue)
            {
                newHover = hoverCandidate.Value;
            }
            else
            {
                newHover = null;
            }

            if (newHover != _pasteHoverGridPos)
            {
                _pasteHoverGridPos = newHover;
                Invalidate();
            }
            return;
        }

        if (e.Button == MouseButtons.Left)
        {
            var dx = e.X - _mouseDownPos.X;
            var dy = e.Y - _mouseDownPos.Y;
            var moved = Math.Abs(dx) + Math.Abs(dy) >= DragThreshold;

            if (ModifierKeys.HasFlag(Keys.Control) && _dragSourcePageIndex >= 0)
            {
                // Ctrl+ドラッグ → ノードD&D
                if (!_isDraggingNode && moved)
                {
                    _isDraggingNode     = true;
                    _isDraggingNodeCopy = ModifierKeys.HasFlag(Keys.Shift);
                }

                if (_isDraggingNode)
                {
                    _dragCurrentGridPos = grid;
                    Invalidate();
                }
            }
            else
            {
                // 通常ドラッグ → ビュー移動
                if (!_isDragging && moved)
                    _isDragging = true;

                if (_isDragging)
                {
                    _viewOffset.X += dx;
                    _viewOffset.Y += dy;
                    _mouseDownPos  = e.Location;
                    ViewOffsetChanged?.Invoke(_viewOffset);
                    Invalidate();
                }
            }
        }
    }

    private void OnMouseUp(object? sender, MouseEventArgs e)
    {
        // 複製モード中の左クリック → 貼り付け先確定
        if (_isPasteMode && e.Button == MouseButtons.Left)
        {
            if (_pasteHoverGridPos.HasValue)
            {
                // 貼り付け先が既存ページか候補位置かを判定
                var targetPageIndex = HitTestPageIndex(e.Location);
                PasteModeConfirmed?.Invoke(
                    _pasteModeSourceIndex,
                    targetPageIndex,
                    _pasteHoverGridPos.Value.X,
                    _pasteHoverGridPos.Value.Y);
            }
            return;
        }

        if (e.Button == MouseButtons.Left)
        {
            if (_isDraggingNode && _dragSourcePageIndex >= 0)
            {
                // ノードD&D確定
                var targetGrid = ScreenToGrid(e.Location);
                if (_isDraggingNodeCopy)
                    NodeCopied?.Invoke(_dragSourcePageIndex, targetGrid.X, targetGrid.Y);
                else
                    NodeMoved?.Invoke(_dragSourcePageIndex, targetGrid.X, targetGrid.Y);
            }
            else if (!_isDragging)
            {
                // クリック確定 → ページ選択
                var pageIndex = HitTestPageIndex(e.Location);
                if (pageIndex >= 0)
                {
                    SelectedPageIndex = pageIndex;
                    PageSelected?.Invoke(pageIndex);
                    Invalidate();
                }
            }
        }

        _isDragging         = false;
        _isDraggingNode     = false;
        _isDraggingNodeCopy = false;
        _dragSourcePageIndex = -1;
        _dragCurrentGridPos  = null;
        Invalidate();
    }

    private void OnDoubleClick(object? sender, EventArgs e)
    {
        var pos       = PointToClient(Cursor.Position);
        var pageIndex = HitTestPageIndex(pos);

        if (pageIndex >= 0)
            PageDoubleClick?.Invoke(pageIndex);
    }

    private void OnMouseWheel(object? sender, MouseEventArgs e)
    {
        if (ModifierKeys.HasFlag(Keys.Control))
        {
            // Ctrl + ホイール → ズーム
            var delta = e.Delta > 0 ? ZoomStep : -ZoomStep;
            _zoom = Math.Clamp(_zoom + delta, ZoomMin, ZoomMax);
            ZoomChanged?.Invoke(_zoom);
            Invalidate();
        }
        else if (ModifierKeys.HasFlag(Keys.Shift))
        {
            // Shift + ホイール → 左右スクロール
            _viewOffset.X += e.Delta > 0 ? 40 : -40;
            ViewOffsetChanged?.Invoke(_viewOffset);
            Invalidate();
        }
        else
        {
            // 通常ホイール → 上下スクロール
            _viewOffset.Y += e.Delta > 0 ? 40 : -40;
            ViewOffsetChanged?.Invoke(_viewOffset);
            Invalidate();
        }
    }
}
