using StageSmith.Application.Commands;
using StageSmith.Core.Models;

namespace StageSmith.Editor;

public partial class MainForm
{
    private void ApplySelectionFill()
    {
        if (_page == null || _selectionTool == null || _selectedTileId < 0)
            return;

        var positions = _selectionTool
            .GetSelectedPositions()
            .Where(p => _page.TileMap.GetTile(p.x, p.y) != _selectedTileId)
            .ToList();

        if (positions.Count == 0)
            return;

        var command = new TilePaintCommand(
            _page.TileMap,
            positions,
            (byte)_selectedTileId
        );

        _commandManager.Execute(command);

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
        _mapView.Invalidate();
    }

    private void DeleteSelection()
    {
        if (_page == null || _selectionTool == null)
            return;

        var positions = _selectionTool
            .GetSelectedPositions()
            .Where(p => _page.TileMap.GetTile(p.x, p.y) != 0)
            .ToList();

        if (positions.Count == 0)
            return;

        var command = new TilePaintCommand(
            _page.TileMap,
            positions,
            0 // 空タイル
        );

        _commandManager.Execute(command);

        _mapView.Invalidate();
    }

    // 選択範囲の移動（コピー or カット） - この機能は、選択範囲を新しい位置に移動し、必要に応じて元の位置をクリアします。
    private void OnSelectionMoveRequested(Rectangle rect, Point offset, bool copy)
    {
        if (_page == null) return;

        var tileMap = _page.TileMap;
        var command = new DragPaintCommand(tileMap);

        var buffer = new byte[rect.Width, rect.Height];

        for (var y = 0; y < rect.Height; y++)
        {
            for (var x = 0; x < rect.Width; x++)
            {
                    buffer[x, y] = tileMap.GetTile(rect.X + x, rect.Y + y);
            }
        }

        var targetPositions = new HashSet<(int x, int y)>();

        for (var y = 0; y < rect.Height; y++)
        {
            for (var x = 0; x < rect.Width; x++)
            {
                var nx = rect.X + x + offset.X;
                var ny = rect.Y + y + offset.Y;

                if (nx < 0 || nx >= tileMap.Width ||
                    ny < 0 || ny >= tileMap.Height)
                    continue;

                targetPositions.Add((nx, ny));

                command.Add(nx, ny, buffer[x, y]);
            }
        }

        if (!copy)
        {
            for (var y = 0; y < rect.Height; y++)
            {
                for (var x = 0; x < rect.Width; x++)
                {
                    var ox = rect.X + x;
                    var oy = rect.Y + y;

                    // ?? ここが核心
                    if (targetPositions.Contains((ox, oy)))
                        continue;

                    command.Add(ox, oy, 0);
                }
            }
        }

        if (command.HasChanges)
            _commandManager.Execute(command);

        _mapView.Invalidate();
    }

    private void ApplyGridState(bool visible)
    {
        _showGrid = visible;
        _mapView.SetShowGrid(_showGrid);
        _showGridButton.Checked = visible;
    }
}
