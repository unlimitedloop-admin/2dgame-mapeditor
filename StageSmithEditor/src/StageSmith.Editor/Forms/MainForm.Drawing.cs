using StageSmith.Core.Constants;

namespace StageSmith.Editor;

public partial class MainForm
{
    private void panelPalette_Paint(object sender, PaintEventArgs e)
    {
        if (_tileset == null) return;

        var g = e.Graphics;
        g.Clear(panelPalette.BackColor);

        var offset = panelPalette.AutoScrollPosition;
        var tileSize = MapConstants.TilePixelSize;
        var tilesPerRow = _tileset.Width / tileSize;
        var totalTiles = (_tileset.Width / tileSize) * (_tileset.Height / tileSize);

        for (var i = 0; i < totalTiles; i++)
        {
            var sx = (i % tilesPerRow) * tileSize;
            var sy = (i / tilesPerRow) * tileSize;
            var srcRect = new Rectangle(sx, sy, tileSize, tileSize);

            var spacing = 2;
            var dx = (i % tilesPerRow) * (tileSize + spacing);
            var dy = (i / tilesPerRow) * (tileSize + spacing);

            var dstRect = new Rectangle(
                dx + offset.X,
                dy + offset.Y,
                tileSize,
                tileSize
            );

            g.DrawImage(_tileset, dstRect, srcRect, GraphicsUnit.Pixel);

            if (i == _selectedTileId)
            {
                var rect = new Rectangle(
                    dstRect.X,
                    dstRect.Y,
                    dstRect.Width - 1,
                    dstRect.Height - 1
                );

                using var pen = new Pen(Color.FromArgb(180, Color.Red), 2);
                g.DrawRectangle(pen, rect);
            }
        }
    }
}
