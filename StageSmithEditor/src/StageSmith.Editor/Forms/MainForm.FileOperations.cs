using StageSmith.Application.Commands;
using StageSmith.Core.Constants;
using StageSmith.Infrastructure.Persistence;

namespace StageSmith.Editor;

public partial class MainForm
{
    private void btnSave_Click(object? sender, EventArgs e)
    {
        var repository = new JsonProjectRepository();
        repository.Save(_project!, "test_project.def");
    }

    private void LoadTest()
    {
        var repository = new JsonProjectRepository();
        _project = repository.Load("test_project.def");

        _stage = _project.Stages.FirstOrDefault();
        _page = _stage?.Pages.FirstOrDefault();

        if (_page != null && !string.IsNullOrEmpty(_stage?.TilesetImagePath))
        {
            _tileset = new Bitmap(_stage.TilesetImagePath);

            var spacing = 2;
            var tileSize = MapConstants.TilePixelSize;
            var tilesPerRow = _tileset.Width / tileSize;
            var tilesPerColumn = _tileset.Height / tileSize;
            var width = tilesPerRow * (tileSize + spacing);
            var height = tilesPerColumn * (tileSize + spacing);

            panelPalette.AutoScrollMinSize = new Size(width, height);

            _mapView.SetTileMap(_page.TileMap);
            _mapView.SetTileset(_tileset);

            _mapView.TilePaintRequested += (x, y) =>
            {
                var current = _page.TileMap.GetTile(x, y);

                if (current == _selectedTileId)
                    return;

                _commandManager.Execute(
                    new SetTileCommand(_page.TileMap, x, y, (byte)_selectedTileId)
                );

                _mapView.Invalidate();
            };

            _mapView.TilePicked += (tileId) =>
            {
                _selectedTileId = tileId;
                panelPalette.Invalidate();
            };
        }

        _mapView.Invalidate();
        panelPalette.Invalidate();
    }
}
