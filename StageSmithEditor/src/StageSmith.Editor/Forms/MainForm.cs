using StageSmith.Core.Models;

namespace StageSmith.Editor
{
    public partial class MainForm : Form
    {
        private EditorProject? _project;
        private Stage? _stage;
        private Page? _page;
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

            // 適当に配置
            _page.TileMap.SetTile(0, 0, 1);
            _page.TileMap.SetTile(1, 0, 2);
            _page.TileMap.SetTile(2, 0, 3);

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

            int tileSize = 16;

            int x = e.X / tileSize;
            int y = e.Y / tileSize;

            // クリックした位置にタイルを配置（例: タイルID 1）
            _page.TileMap.SetTile(x, y, 1);
            panel1.Invalidate(); // 再描画
        }
    }
}
