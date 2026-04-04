namespace StageSmith.Editor;

public partial class MainForm
{
    private void panel1_Paint(object sender, PaintEventArgs e)
    {
        if (_tileset == null) return;
        if (e.Graphics == null) return;
        if (_page == null) return;

        var g = e.Graphics;
        var tileSize = 16;

        for (var y = 0; y < _page.TileMap.Height; y++)
        {
            for (var x = 0; x < _page.TileMap.Width; x++)
            {
                int tileId = _page.TileMap.GetTile(x, y);
                var tilesPerRow = _tileset.Width / tileSize;

                var sx = (tileId % tilesPerRow) * tileSize;
                var sy = (tileId / tilesPerRow) * tileSize;

                var srcRect = new Rectangle(sx, sy, tileSize, tileSize);
                var dstRect = new Rectangle(x * tileSize, y * tileSize, tileSize, tileSize);

                g.DrawImage(_tileset, dstRect, srcRect, GraphicsUnit.Pixel);
            }
        }

        if (_showGrid)
        {
            DrawGrid(e.Graphics);
        }
    }

    private void DrawGrid(Graphics g)
    {
        if (_page == null) return;

        var tileSize = 16;

        var width = _page.TileMap.Width * tileSize;
        var height = _page.TileMap.Height * tileSize;

        using var pen = new Pen(Color.FromArgb(80, Color.White));

        // 縦線
        for (var x = 0; x <= _page.TileMap.Width; x++)
        {
            var px = x * tileSize;
            g.DrawLine(pen, px, 0, px, height);
        }

        // 横線
        for (var y = 0; y <= _page.TileMap.Height; y++)
        {
            var py = y * tileSize;
            g.DrawLine(pen, 0, py, width, py);
        }
    }

    private void panelPalette_Paint(object sender, PaintEventArgs e)
    {
        if (_tileset == null) return;

        var g = e.Graphics;
        g.Clear(panelPalette.BackColor);

        var offset = panelPalette.AutoScrollPosition;
        var tileSize = 16;
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
