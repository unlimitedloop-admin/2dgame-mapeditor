using StageSmith.Application.Commands;
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

        _fillTool = new FillTool(
            () => _page?.TileMap,
            positions =>
            {
                if (_page == null || _selectedTileId < 0) return;

                var commands = new List<ICommand>();

                foreach (var (px, py) in positions)
                {
                    var current = _page.TileMap.GetTile(px, py);

                    if (current == _selectedTileId)
                        continue;

                    commands.Add(new SetTileCommand(
                        _page.TileMap,
                        px,
                        py,
                        (byte)_selectedTileId));
                }

                if (commands.Count > 0)
                {
                    _commandManager.Execute(new CompositeCommand(commands));
                }

                _mapView.Invalidate();
            });

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
}
