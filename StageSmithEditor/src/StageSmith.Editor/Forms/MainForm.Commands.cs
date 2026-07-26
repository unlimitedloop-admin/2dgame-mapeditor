using StageSmith.Application.Commands;
using StageSmith.Core.Constants;
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

        var rects = _selectionTool.SelectionRects;
        if (rects.Count == 0) return;

        var minX = rects.Min(r => r.Left);
        var minY = rects.Min(r => r.Top);
        var maxX = rects.Max(r => r.Right);
        var maxY = rects.Max(r => r.Bottom);

        var width = maxX - minX;
        var height = maxY - minY;
        var tiles = new byte?[width, height];

        var tileMap = _page.TileMap;

        // 各矩形の範囲だけ値を埋める。矩形間の隙間は null のまま（＝データなし）。
        foreach (var rect in rects)
        {
            for (var y = rect.Top; y < rect.Bottom; y++)
            {
                for (var x = rect.Left; x < rect.Right; x++)
                {
                    tiles[x - minX, y - minY] = tileMap.GetTile(x, y);
                }
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
                var tile = _clipboard.Tiles[x, y];
                if (tile == null) continue; // 隙間はスキップ＝ペースト先の既存タイルを保持

                var mapX = startX + x;
                var mapY = startY + y;

                if (mapX < 0 || mapX >= tileMap.Width ||
                    mapY < 0 || mapY >= tileMap.Height)
                    continue; // 範囲外は切り捨て

                command.Add(mapX, mapY, tile.Value);
            }
        }

        if (command.HasChanges)
        {
            _commandManager.Execute(command);
            _mapView.Invalidate();
        }
    }

    private void PasteSelection()
    {
        if (_clipboard == null) return;

        // 選択範囲があれば、その外接矩形の左上を貼り付け基準点にする
        if (_selectionTool != null && _selectionTool.SelectionRects.Count > 0)
        {
            var minX = _selectionTool.SelectionRects.Min(r => r.Left);
            var minY = _selectionTool.SelectionRects.Min(r => r.Top);
            PasteSelection(minX, minY);
            return;
        }

        // 選択範囲が無い場合のみ、ホバー位置にフォールバックする
        var pos = _mapView.GetHoverTile();
        if (pos.X < 0 || pos.Y < 0) return;
        PasteSelection(pos.X, pos.Y);
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

    private void SelectAllTiles()
    {
        if (_page == null || _selectionTool == null) return;

        SetToolMode(EditorToolMode.Selection);
        _selectionTool.SelectAll(_page.TileMap.Width, _page.TileMap.Height);
        _mapView.Invalidate();
    }

    private void SelectAllSameTile()
    {
        if (_selectedTileId < 0) return;
        SelectTilesMatching(_selectedTileId);
    }

    private void SelectEmptyTile()
    {
        SelectTilesMatching(0);
    }

    /// <summary>
    /// 現在ページ内で指定タイルIDと一致する全マスを、複数選択範囲として確定する。
    /// </summary>
    private void SelectTilesMatching(int tileId)
    {
        if (_page == null || _selectionTool == null) return;

        var tileMap = _page.TileMap;
        var rects = new List<Rectangle>();

        for (var y = 0; y < tileMap.Height; y++)
        {
            for (var x = 0; x < tileMap.Width; x++)
            {
                if (tileMap.GetTile(x, y) == tileId)
                    rects.Add(new Rectangle(x, y, 1, 1));
            }
        }

        if (rects.Count == 0) return;

        SetToolMode(EditorToolMode.Selection);
        _selectionTool.SetSelectionRects(rects);
        _mapView.Invalidate();
    }

    private void InvertSelection()
    {
        if (_page == null || _selectionTool == null) return;

        var tileMap = _page.TileMap;
        var selected = _selectionTool.GetSelectedPositions().ToHashSet();

        var rects = new List<Rectangle>();

        for (var y = 0; y < tileMap.Height; y++)
        {
            for (var x = 0; x < tileMap.Width; x++)
            {
                if (!selected.Contains((x, y)))
                    rects.Add(new Rectangle(x, y, 1, 1));
            }
        }

        if (rects.Count == 0)
        {
            // 元の選択範囲がページ全体だった場合、反転結果は「選択なし」になる
            _selectionTool.ClearSelection();
            _mapView.Invalidate();
            return;
        }

        SetToolMode(EditorToolMode.Selection);
        _selectionTool.SetSelectionRects(rects);
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
}
