using StageSmith.Application.Commands;

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

        // 選択範囲を取得する、現状は選択範囲は矩形のみをサポート
        var rect = _mapView.SelectionRect;
        if (rect == null) return;

        var tileMap = _page.TileMap;

        _clipboardWidth = rect.Value.Width;
        _clipboardHeight = rect.Value.Height;

        _clipboardTiles = new byte[_clipboardWidth, _clipboardHeight];

        for (var y = 0; y < _clipboardHeight; y++)
        {
            for (var x = 0; x < _clipboardWidth; x++)
            {
                var mapX = rect.Value.X + x;
                var mapY = rect.Value.Y + y;

                _clipboardTiles[x, y] = tileMap.GetTile(mapX, mapY);
            }
        }
    }

    private void PasteClipboard()
    {
        if (_page == null) return;
        if (_clipboardTiles == null) return;

        var tileMap = _page.TileMap;
        var start = _mapView.GetHoverTile();

        if (start.X < 0 || start.Y < 0)
            return;

        var commands = new List<ICommand>();

        for (var y = 0; y < _clipboardHeight; y++)
        {
            for (var x = 0; x < _clipboardWidth; x++)
            {
                var mapX = start.X + x;
                var mapY = start.Y + y;

                // 範囲チェック🔥
                if (mapX < 0 || mapX >= tileMap.Width ||
                    mapY < 0 || mapY >= tileMap.Height)
                    continue;

                var newValue = _clipboardTiles[x, y];
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
}
