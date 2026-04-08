using StageSmith.Application.Commands;
using StageSmith.Editor.Tools;

namespace StageSmith.Editor;

public partial class MainForm
{
    private void InitializeTools()
    {
        _penTool = new PenTool((x, y) =>
        {
            if (_page == null) return;
            if (_selectedTileId < 0) return;

            var current = _page.TileMap.GetTile(x, y);

            if (current == _selectedTileId)
                return;

            _commandManager.Execute(
                new SetTileCommand(_page.TileMap, x, y, (byte)_selectedTileId)
            );

            _mapView.Invalidate();
        });

        _pickerTool = new PickerTool(
            (x, y) => _page?.TileMap.GetTile(x, y) ?? -1,
            tileId =>
            {
                //if (tileId < 0) return;

                _selectedTileId = tileId;
                _tilePalette.SetSelected(tileId);
                _mapView.PreviewTileId = tileId;
            });
    }
}
