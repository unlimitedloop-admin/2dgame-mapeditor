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

        if (!string.IsNullOrEmpty(_stage?.TilesetImagePath))
        {
            _tileset = new Bitmap(_stage.TilesetImagePath);

            var spacing = 2;
            var tileSize = 16;
            var tilesPerRow = _tileset.Width / tileSize;
            var tilesPerColumn = _tileset.Height / tileSize;
            var width = tilesPerRow * (tileSize + spacing);
            var height = tilesPerColumn * (tileSize + spacing);

            panelPalette.AutoScrollMinSize = new Size(width, height);
        }

        panel1.Invalidate();
        panelPalette.Invalidate();
    }
}
