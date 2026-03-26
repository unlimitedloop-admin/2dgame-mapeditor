using StageSmith.Core.Models;

namespace StageSmith.Editor
{
    public partial class MainForm : Form
    {
        private EditorProject? _project;
        private Stage? _stage;
        private Page? _page;
        private int _selectedTileId = -1; // 仮の選択タイルID
        private Bitmap? _tileset;

        public MainForm()
        {
            InitializeComponent();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            // テストデータ
            _project = new EditorProject();

            _stage = _project.AddStage("Stage 1");
            _stage.TilesetImagePath = @"Assets\DEMOSTAGE2_N0_ALL_PATTERN.png";

            _page = _stage.AddPage("Start");

            // タイルセット読み込み
            _tileset = new Bitmap(_stage.TilesetImagePath);

            panel1.Invalidate(); // 再描画
        }

        private void panel1_Paint(object sender, PaintEventArgs e)
        {
            if (_tileset == null) return;
            if (e.Graphics == null) return;
            if (_page == null) return;

            var g = e.Graphics;

            int tileSize = 16;

            for (int y = 0; y < _page.TileMap.Height; y++)
            {
                for (int x = 0; x < _page.TileMap.Width; x++)
                {
                    int tileId = _page.TileMap.GetTile(x, y);

                    // タイル番号 → 画像内位置
                    int tilesPerRow = _tileset.Width / tileSize;

                    int sx = (tileId % tilesPerRow) * tileSize;
                    int sy = (tileId / tilesPerRow) * tileSize;

                    var srcRect = new Rectangle(sx, sy, tileSize, tileSize);
                    var dstRect = new Rectangle(x * tileSize, y * tileSize, tileSize, tileSize);

                    g.DrawImage(_tileset, dstRect, srcRect, GraphicsUnit.Pixel);
                }
            }
        }

        private void panel1_MouseDown(object sender, MouseEventArgs e)
        {
            if (_page == null) return;
            if (_selectedTileId < 0) return; // タイルが選択されていない

            int tileSize = 16;

            int x = e.X / tileSize;
            int y = e.Y / tileSize;

            // クリックした位置にタイルを配置
            var selectTile = (byte)_selectedTileId;
            _page.TileMap.SetTile(x, y, selectTile);
            panel1.Invalidate(); // 再描画
        }

        private void panelPalette_Paint(object sender, PaintEventArgs e)
        {
            if (_tileset == null) return;

            var g = e.Graphics;

            int tileSize = 16;
            int tilesPerRow = _tileset.Width / tileSize;
            int totalTiles = (_tileset.Width / tileSize) * (_tileset.Height / tileSize);

            for (int i = 0; i < totalTiles; i++)
            {
                int sx = (i % tilesPerRow) * tileSize;
                int sy = (i / tilesPerRow) * tileSize;

                var srcRect = new Rectangle(sx, sy, tileSize, tileSize);

                int dx = (i % tilesPerRow) * tileSize;
                int dy = (i / tilesPerRow) * tileSize;

                var dstRect = new Rectangle(dx, dy, tileSize, tileSize);

                g.DrawImage(_tileset, dstRect, srcRect, GraphicsUnit.Pixel);

                // 選択枠
                if (i == _selectedTileId)
                {
                    g.DrawRectangle(Pens.Red, dstRect);
                }
            }
        }

        private void panelPalette_MouseDown(object sender, MouseEventArgs e)
        {
            if (_tileset == null) return;

            int tileSize = 16;
            int tilesPerRow = _tileset.Width / tileSize;

            int x = e.X / tileSize;
            int y = e.Y / tileSize;

            int tileId = y * tilesPerRow + x;

            _selectedTileId = tileId;
            panelPalette.Invalidate(); // 再描画
        }
    }
}
