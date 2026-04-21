using StageSmith.Application.Commands;
using StageSmith.Core.Constants;
using StageSmith.Editor.Tools;

namespace StageSmith.Editor;

public partial class MainForm
{
    private PenTool? _penTool;
    private PickerTool? _pickerTool;
    private SelectionTool? _selectionTool;

    private DragPaintCommand? _currentDragCommand;

    private EditorToolMode _currentMode = EditorToolMode.Pen;

    // =========================
    // 初期化
    // =========================

    private void InitializeTools()
    {
        // ===== Pen =====
        _penTool = new PenTool((x, y) =>
        {
            if (_page == null || _selectedTileId < 0)
                return;

            _currentDragCommand?.Add(x, y, (byte)_selectedTileId);

            _mapView.Invalidate();
        });

        // ===== Picker =====
        _pickerTool = new PickerTool(
            (x, y) => _page?.TileMap.GetTile(x, y) ?? -1,
            tileId =>
            {
                if (tileId < 0) return;

                _selectedTileId = tileId;
                _tilePalette.SetSelected(tileId);

                _mapView.PreviewTileId = tileId;
            });

        // ===== Selection =====
        _selectionTool = new SelectionTool();
        _selectionTool.SelectionChanged += () => _mapView.Invalidate();

        // ===== MapView接続 =====
        _mapView.CurrentTool = _penTool;
        _mapView.PickerTool = _pickerTool;
        _mapView.SelectionTool = _selectionTool;

        // ===== Drag Command 制御 =====
        _mapView.MouseDown += (s, e) =>
        {
            if (e.Button == MouseButtons.Left &&
                _page != null &&
                _currentMode == EditorToolMode.Pen)
            {
                _currentDragCommand = new DragPaintCommand(_page.TileMap);
            }
        };

        _mapView.MouseUp += (s, e) =>
        {
            if (_currentDragCommand != null &&
                _currentDragCommand.HasChanges)
            {
                _commandManager.Execute(_currentDragCommand);
                _mapView.Invalidate();
            }

            _currentDragCommand = null;
        };
    }

    // =========================
    // モード切替
    // =========================

    private void SetToolMode(EditorToolMode mode)
    {
        if (_currentMode == mode)
            return;

        _currentMode = mode;

        switch (mode)
        {
            case EditorToolMode.Pen:
                _mapView.CurrentTool = _penTool;
                _selectionTool?.ClearSelection();
                break;

            case EditorToolMode.Selection:
                _mapView.CurrentTool = _selectionTool;
                break;
        }

        UpdateModeUI();

        _mapView.Invalidate();
    }

    private void UpdateModeUI()
    {
        // 必要ならここでステータス表示など
        // lblMode.Text = _currentMode.ToString();
    }
}
