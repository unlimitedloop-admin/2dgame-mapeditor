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
        if (_selectionTool == null) return;

        var rect = _selectionTool.SelectionRect;
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
        if (_page == null || _selectionTool == null)
            return;

        var rect = _selectionTool.SelectionRect;
        if (rect == null) return;

        var tileMap = _page.TileMap;

        var width = rect.Value.Width;
        var height = rect.Value.Height;
        var tiles = new byte[width, height];

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var mapX = rect.Value.X + x;
                var mapY = rect.Value.Y + y;
                tiles[x, y] = tileMap.GetTile(mapX, mapY);
            }
        }

        _clipboard = new ClipboardData(tiles);

        _selectionTool.SetSelectionCopied(true);

        UpdatePastePreviewState();
    }

    private void PasteSelection(int startX, int startY)
    {
        if (_page == null || _clipboard == null)
            return;

        var tileMap = _page.TileMap;
        var command = new DragPaintCommand(tileMap);

        for (var y = 0; y < _clipboard.Height; y++)
        {
            for (var x = 0; x < _clipboard.Width; x++)
            {
                var mapX = startX + x;
                var mapY = startY + y;

                if (mapX < 0 || mapX >= tileMap.Width ||
                    mapY < 0 || mapY >= tileMap.Height)
                    continue;

                command.Add(mapX, mapY, _clipboard.Tiles[x, y]);
            }
        }

        if (command.HasChanges)
        {
            _commandManager.Execute(command);
            _mapView.Invalidate();
        }
    }

    private void ClearSelection()
    {
        _selectionTool?.ClearSelection();

        // プレビューも消す
        _mapView.SetPastePreviewBitmap(null);

        _mapView.Invalidate();
    }

    private void DeleteSelection()
    {
        if (_page == null) return;
        if (_selectionTool == null) return;

        var rect = _selectionTool.SelectionRect;
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

    private void UpdatePastePreviewBitmap()
    {
        if (_clipboard == null || _tileset == null)
        {
            _mapView.SetPastePreviewBitmap(null);
            return;
        }

        var tileSize = MapConstants.TilePixelSize;
        var bmp = new Bitmap(_clipboard.Width * tileSize, _clipboard.Height * tileSize);

        using var g = Graphics.FromImage(bmp);

        var tilesPerRow = _tileset.Width / tileSize;

        for (var y = 0; y < _clipboard.Height; y++)
        {
            for (var x = 0; x < _clipboard.Width; x++)
            {
                var tileId = _clipboard.Tiles[x, y];

                var sx = (tileId % tilesPerRow) * tileSize;
                var sy = (tileId / tilesPerRow) * tileSize;

                var src = new Rectangle(sx, sy, tileSize, tileSize);
                var dst = new Rectangle(x * tileSize, y * tileSize, tileSize, tileSize);

                g.DrawImage(_tileset, dst, src, GraphicsUnit.Pixel);
            }
        }

        _mapView.SetPastePreviewBitmap(bmp);
    }

    private void UpdatePastePreviewState()
    {
        var show = _clipboard != null && _isCtrlPressed;

        _mapView.ShowPastePreview = show;

        if (show)
        {
            UpdatePastePreviewBitmap();
        }
        else
        {
            _mapView.SetPastePreviewBitmap(null);
        }

        _mapView.Invalidate();
    }

    private void OnSelectionMoveRequested(Rectangle rect, Point offset, bool copy)
    {
        if (_page == null) return;

        var tileMap = _page.TileMap;
        var command = new DragPaintCommand(tileMap);

        var temp = new Dictionary<(int x, int y), byte>();

        // 元データ保存
        for (var y = rect.Top; y < rect.Bottom; y++)
        {
            for (var x = rect.Left; x < rect.Right; x++)
            {
                temp[(x, y)] = tileMap.GetTile(x, y);
            }
        }

        // 元位置クリア (コピーの場合は残す)
        if (!copy)
        {
            foreach (var kv in temp)
                command.Add(kv.Key.x, kv.Key.y, 0);
        }

        // 新位置へ
        foreach (var kv in temp)
        {
            var nx = kv.Key.x + offset.X;
            var ny = kv.Key.y + offset.Y;

            if (nx < 0 || nx >= tileMap.Width ||
                ny < 0 || ny >= tileMap.Height)
                continue;

            command.Add(nx, ny, kv.Value);
        }

        if (command.HasChanges)
        {
            _commandManager.Execute(command);
        }

        // 選択枠も移動
        var newRect = new Rectangle(
            rect.X + offset.X,
            rect.Y + offset.Y,
            rect.Width,
            rect.Height
        );

        _selectionTool?.SetSelectionRect(newRect);
        _mapView.Invalidate();
    }
}
