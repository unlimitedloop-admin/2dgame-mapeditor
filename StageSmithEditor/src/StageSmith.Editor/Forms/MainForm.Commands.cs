using StageSmith.Application.Commands;
using StageSmith.Core.Constants;
using StageSmith.Core.Models;

namespace StageSmith.Editor;

public partial class MainForm
{
    private void btnUndo_Click(object sender, EventArgs e)
    {
        _commandManager.Undo();
        _mapView.Invalidate();
    }

    private void btnRedo_Click(object sender, EventArgs e)
    {
        _commandManager.Redo();
        _mapView.Invalidate();
    }

    private void ApplySelectionFill()
    {
        if (_page == null) return;
        if (_selectedTileId < 0) return;

        var rect = _mapView.SelectionRect;
        if (rect == null) return;

        var tileMap = _page.TileMap;

        var commands = new List<ICommand>();

        for (var y = rect.Value.Top; y < rect.Value.Bottom; y++)
        {
            for (var x = rect.Value.Left; x < rect.Value.Right; x++)
            {
                var current = tileMap.GetTile(x, y);

                if (current == _selectedTileId)
                    continue;

                commands.Add(new SetTileCommand(tileMap, x, y, (byte)_selectedTileId));
            }
        }

        if (commands.Count > 0)
        {
            _commandManager.Execute(new CompositeCommand(commands));
        }

        _mapView.Invalidate();
    }

    private void CopySelection()
    {
        if (_page == null) return;

        var rect = _mapView.SelectionRect;
        if (rect == null) return;

        var tileMap = _page.TileMap;
        var tiles = new byte[rect.Value.Width, rect.Value.Height];

        for (var y = 0; y < rect.Value.Height; y++)
        {
            for (var x = 0; x < rect.Value.Width; x++)
            {
                tiles[x, y] = tileMap.GetTile(rect.Value.X + x, rect.Value.Y + y);
            }
        }

        _clipboard = new ClipboardData(tiles);
        _mapView.SetSelectionCopied(true);
        _mapView.SetPastePreview(BuildPreviewBitmap(_clipboard));
    }

    private void PasteClipboard()
    {
        if (_page == null) return;
        if (_clipboard == null) return;         // null チェックが1行になる

        var rect = _mapView.SelectionRect;
        if (rect == null) return;

        var tileMap = _page.TileMap;
        var start = rect.Value.Location;
        var commands = new List<ICommand>();

        for (var y = 0; y < _clipboard.Height; y++)
        {
            for (var x = 0; x < _clipboard.Width; x++)
            {
                var mapX = start.X + x;
                var mapY = start.Y + y;

                if (mapX < 0 || mapX >= tileMap.Width ||
                    mapY < 0 || mapY >= tileMap.Height)
                    continue;

                var newValue = _clipboard.Tiles[x, y];
                var current = tileMap.GetTile(mapX, mapY);

                if (current == newValue)
                    continue;

                commands.Add(new SetTileCommand(tileMap, mapX, mapY, newValue));
            }
        }

        if (commands.Count > 0)
        {
            _commandManager.Execute(new CompositeCommand(commands));
        }

        _mapView.Invalidate();
    }

    private void DeleteSelection()
    {
        if (_page == null) return;

        var rect = _mapView.SelectionRect;
        if (rect == null) return;

        var tileMap = _page.TileMap;
        var commands = new List<ICommand>();

        for (var y = rect.Value.Top; y < rect.Value.Bottom; y++)
        {
            for (var x = rect.Value.Left; x < rect.Value.Right; x++)
            {
                if (tileMap.GetTile(x, y) == 0)
                    continue;

                commands.Add(new SetTileCommand(tileMap, x, y, 0));
            }
        }

        if (commands.Count > 0)
        {
            _commandManager.Execute(new CompositeCommand(commands));
        }

        _mapView.Invalidate();
    }

    private Bitmap BuildPreviewBitmap(ClipboardData clipboard)
    {
        var tileSize = MapConstants.TilePixelSize;
        var bmp = new Bitmap(
            clipboard.Width * tileSize,
            clipboard.Height * tileSize
        );

        using var g = Graphics.FromImage(bmp);

        for (var y = 0; y < clipboard.Height; y++)
        {
            for (var x = 0; x < clipboard.Width; x++)
            {
                var tileId = clipboard.Tiles[x, y];
                var tilesPerRow = _tileset!.Width / tileSize;

                var sx = (tileId % tilesPerRow) * tileSize;
                var sy = (tileId / tilesPerRow) * tileSize;

                g.DrawImage(
                    _tileset,
                    new Rectangle(x * tileSize, y * tileSize, tileSize, tileSize),
                    new Rectangle(sx, sy, tileSize, tileSize),
                    GraphicsUnit.Pixel
                );
            }
        }

        return bmp;
    }
}
