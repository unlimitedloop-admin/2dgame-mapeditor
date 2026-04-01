using StageSmith.Application.Commands;
using StageSmith.Core.Models;

namespace StageSmith.Editor;

public partial class MainForm : Form
{
    private EditorProject? _project;
    private Stage? _stage;
    private Page? _page;
    private int _selectedTileId = -1; // 仮の選択タイルID
    private Bitmap? _tileset;

    private CommandManager _commandManager = new();

    public MainForm()
    {
        InitializeComponent();
        this.KeyPreview = true;
        this.KeyDown += MainForm_KeyDown;
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

        // パレットのスクロール範囲を設定
        panelPalette.AutoScrollMinSize = new Size(_tileset.Width, _tileset.Height);

        panel1.Invalidate(); // 再描画
    }

    private void MainForm_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Control && e.KeyCode == Keys.Z)
        {
            _commandManager.Undo();
            panel1.Invalidate();
            e.SuppressKeyPress = true;
        }
        else if (e.Control && e.KeyCode == Keys.Y)
        {
            _commandManager.Redo();
            panel1.Invalidate();
            e.SuppressKeyPress = true;
        }
    }

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

        var tileSize = 16;

        int x = e.X / tileSize;
        int y = e.Y / tileSize;

        // 範囲チェック（重要）
        if (x < 0 || x >= _page.TileMap.Width ||
            y < 0 || y >= _page.TileMap.Height)
            return;

        // 左クリック → 描画
        if (e.Button == MouseButtons.Left)
        {
            _commandManager.Execute(
                new SetTileCommand(_page.TileMap, x, y, (byte)_selectedTileId)
            );
        }
        // 右クリック → スポイト
        else if (e.Button == MouseButtons.Right)
        {
            _selectedTileId = _page.TileMap.GetTile(x, y);

            panelPalette.Invalidate(); // パレット更新
        }

        panel1.Invalidate();
    }

    private void panelPalette_Paint(object sender, PaintEventArgs e)
    {
        if (_tileset == null) return;

        var g = e.Graphics;
        g.Clear(panelPalette.BackColor);

        var offset = panelPalette.AutoScrollPosition;

        var tileSize = 16;
        int tilesPerRow = _tileset.Width / tileSize;
        int totalTiles = (_tileset.Width / tileSize) * (_tileset.Height / tileSize);

        for (int i = 0; i < totalTiles; i++)
        {
            int sx = (i % tilesPerRow) * tileSize;
            int sy = (i / tilesPerRow) * tileSize;

            var srcRect = new Rectangle(sx, sy, tileSize, tileSize);

            int dx = (i % tilesPerRow) * tileSize;
            int dy = (i / tilesPerRow) * tileSize;

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

    private void panelPalette_MouseDown(object sender, MouseEventArgs e)
    {
        if (_tileset == null) return;

        var tileSize = 16;
        var offset = panelPalette.AutoScrollPosition;

        int x = (e.X - offset.X) / tileSize;
        int y = (e.Y - offset.Y) / tileSize;

        int tilesPerRow = _tileset.Width / tileSize;
        int tileId = y * tilesPerRow + x;

        _selectedTileId = tileId;
        panelPalette.Invalidate(); // 再描画
    }
}
