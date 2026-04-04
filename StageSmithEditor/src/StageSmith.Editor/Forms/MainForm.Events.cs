using StageSmith.Application.Commands;

namespace StageSmith.Editor;

public partial class MainForm
{
    private void MainForm_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Control && e.KeyCode == Keys.Z)
        {
            _commandManager.Undo();
            panel1.Invalidate();
            e.SuppressKeyPress = true;
        }
        else if (e.Control && e.KeyCode == Keys.Y)
        {
            _commandManager.Redo();
            panel1.Invalidate();
            e.SuppressKeyPress = true;
        }
        else if (e.Control && e.Shift && e.KeyCode == Keys.S)
        {
            btnSave_Click(sender!, e);
            e.SuppressKeyPress = true;
        }
    }

    private void panel1_MouseDown(object? sender, MouseEventArgs e)
    {
        if (_page == null) return;
        if (_selectedTileId < 0) return;

        var tileSize = 16;
        var x = e.X / tileSize;
        var y = e.Y / tileSize;

        if (x < 0 || x >= _page.TileMap.Width ||
            y < 0 || y >= _page.TileMap.Height)
            return;

        if (e.Button == MouseButtons.Left)
        {
            _isMouseDown = true;
            _commandManager.Execute(
                new SetTileCommand(_page.TileMap, x, y, (byte)_selectedTileId)
            );
        }
        else if (e.Button == MouseButtons.Right)
        {
            _selectedTileId = _page.TileMap.GetTile(x, y);
            panelPalette.Invalidate();
        }

        panel1.Invalidate();
    }

    private void panel1_MouseMove(object? sender, MouseEventArgs e)
    {
        if (_page == null) return;
        if (!_isMouseDown) return;

        var tileSize = 16;
        var x = e.X / tileSize;
        var y = e.Y / tileSize;

        if (x < 0 || x >= _page.TileMap.Width ||
            y < 0 || y >= _page.TileMap.Height)
            return;

        if (_page.TileMap.GetTile(x, y) == _selectedTileId)
            return;

        _commandManager.Execute(
            new SetTileCommand(_page.TileMap, x, y, (byte)_selectedTileId)
        );

        panel1.Invalidate();
    }

    private void panel1_MouseUp(object? sender, MouseEventArgs e)
    {
        _isMouseDown = false;
    }

    private void panelPalette_MouseDown(object? sender, MouseEventArgs e)
    {
        if (_tileset == null) return;

        var spacing = 2;
        var tileSize = 16;
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
        panel1.Invalidate();
    }
}
