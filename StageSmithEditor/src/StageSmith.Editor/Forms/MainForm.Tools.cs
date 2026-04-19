using StageSmith.Application.Commands;
using StageSmith.Core.Constants;
using StageSmith.Editor.Tools;

namespace StageSmith.Editor;

public partial class MainForm
{
    private void InitializeTools()
    {
        _penTool = new PenTool(
            (x, y) =>
            {
                if (_page == null || _selectedTileId < 0)
                    return;

                _currentDragCommand?.Add(x, y, (byte)_selectedTileId);

                _mapView.Invalidate();
            });

        _pickerTool = new PickerTool(
            (x, y) => _page?.TileMap.GetTile(x, y) ?? -1,
            tileId =>
            {
                _selectedTileId = tileId;
                _tilePalette.SetSelected(tileId);
                _mapView.PreviewTileId = tileId;
            });

        _selectionTool = new SelectionTool(_mapView);

        _mapView.MouseDown += (s, e) =>
        {
            if (e.Button == MouseButtons.Left && _page != null)
            {
                _currentDragCommand = new DragPaintCommand(_page.TileMap);
            }
        };

        _mapView.MouseUp += (s, e) =>
        {
            if (_currentDragCommand != null && _currentDragCommand.HasChanges)
            {
                _commandManager.Execute(_currentDragCommand);
                _mapView.Invalidate();
            }

            _currentDragCommand = null;
        };
    }

    private void SetToolMode(EditorToolMode mode)
    {
        _currentMode = mode;
        switch (mode)
        {
            case EditorToolMode.Pen:
                _mapView.CurrentTool = _penTool;
                _mapView.ClearSelection();   // ペンに戻ったら選択解除
                break;
            case EditorToolMode.Selection:
                _mapView.CurrentTool = _selectionTool;
                break;
        }
    }
}
