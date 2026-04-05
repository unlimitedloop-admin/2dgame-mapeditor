using StageSmith.Application.Commands;
using StageSmith.Core.Constants;

namespace StageSmith.Editor;

public partial class MainForm
{
    private void MainForm_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Control && e.KeyCode == Keys.Z)
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
        else if (e.Control && e.Shift && e.KeyCode == Keys.S)
        {
            btnSave_Click(sender!, e);
            e.SuppressKeyPress = true;
        }
    }

    private void panelPalette_MouseDown(object? sender, MouseEventArgs e)
    {
        if (_tileset == null) return;

        var spacing = 2;
        var tileSize = MapConstants.TilePixelSize;
        var offset = panelPalette.AutoScrollPosition;

        var x = (e.X - offset.X) / (tileSize + spacing);
        var y = (e.Y - offset.Y) / (tileSize + spacing);

        var tilesPerRow = _tileset.Width / tileSize;
        var tileId = y * tilesPerRow + x;

        _selectedTileId = tileId;
        panelPalette.Invalidate();
    }

    private void btnGrid_Click(object? sender, EventArgs e)
    {
        _showGrid = !_showGrid;
        _mapView.SetShowGrid(_showGrid);
    }
}
