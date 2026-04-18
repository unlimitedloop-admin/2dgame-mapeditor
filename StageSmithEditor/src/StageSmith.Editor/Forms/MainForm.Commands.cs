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
}
