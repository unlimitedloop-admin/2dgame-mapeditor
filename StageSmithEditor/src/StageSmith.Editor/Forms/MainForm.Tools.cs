using StageSmith.Core.Constants;
using System.Drawing;
using System.Windows.Forms;

namespace StageSmith.Editor;

public partial class MainForm
{
    //========================
    // ToolStrip UI
    //========================
    private ToolStrip _editorToolStrip = null!;

    private ToolStripButton _newButton = null!;
    private ToolStripButton _openButton = null!;
    private ToolStripButton _saveButton = null!;
    private ToolStripButton _exportBinButton = null!;
    private ToolStripButton _openTileSetButton = null!;
    private ToolStripButton _undoButton = null!;
    private ToolStripButton _redoButton = null!;
    private ToolStripButton _penButton = null!;
    private ToolStripButton _selectionButton = null!;

    //========================
    // 初期化
    //========================
    private void InitializeToolStrip()
    {
        _editorToolStrip = new ToolStrip
        {
            Dock = DockStyle.Top,
            GripStyle = ToolStripGripStyle.Hidden,
            ImageScalingSize = new Size(24, 24)
        };

        _newButton = CreateButton("New", "新規プロジェクト (Ctrl+N)", StageSmithEditor.Properties.Resources.icons8_プロジェクト_30);
        _openButton = CreateButton("Open", "プロジェクトを開く (Ctrl+O)", StageSmithEditor.Properties.Resources.icons8_ファイルを開く_30);
        _saveButton = CreateButton("Save", "保存 (Ctrl+Shift+S)", StageSmithEditor.Properties.Resources.icons8_上書き保存_30);

        _exportBinButton = CreateButton("ExportBin", "バイナリ出力", StageSmithEditor.Properties.Resources.icons8_バイナリファイル_30);
        _openTileSetButton = CreateButton("OpenTileSet", "タイル画像を開く", StageSmithEditor.Properties.Resources.icons8_画像を開く_30);

        _undoButton = CreateButton("Undo", "元に戻す (Ctrl+Z)", StageSmithEditor.Properties.Resources.icons8_元に戻す_30);
        _redoButton = CreateButton("Redo", "やり直し (Ctrl+Y)", StageSmithEditor.Properties.Resources.icons8_やり直す_30);

        _penButton = CreateButton("Pen", "ペン (P)", StageSmithEditor.Properties.Resources.icons8_鉛筆_24, true);
        _selectionButton = CreateButton("Selection", "選択 (S)", StageSmithEditor.Properties.Resources.icons8_選択_24, true);

        //========================
        // イベント
        //========================
        _newButton.Click += (_, _) =>
        {
            NewProject();
        };

        _openButton.Click += (_, _) =>
        {
            OpenProject();
        };

        _saveButton.Click += (_, _) =>
        {
            SaveProject();
        };

        _exportBinButton.Click += (_, _) =>
        {
            ExportCurrentStageBin();
        };

        _undoButton.Click += (_, _) =>
        {
            _commandManager.Undo();
            _mapView.Invalidate();
        };

        _redoButton.Click += (_, _) =>
        {
            _commandManager.Redo();
            _mapView.Invalidate();
        };

        _penButton.Click += (_, _) =>
        {
            if (_penTool != null)
                ChangeTool(_penTool);
        };

        _selectionButton.Click += (_, _) =>
        {
            if (_selectionTool != null)
                ChangeTool(_selectionTool);
        };

        _openTileSetButton.Click += (_, _) =>
        {
            OpenTilesetImage();
        };

        //========================
        // UI構築
        //========================
        _editorToolStrip.Items.AddRange(new ToolStripItem[]
        {
            _newButton,
            _openButton,
            _saveButton,
            new ToolStripSeparator(),
            _exportBinButton,
            new ToolStripSeparator(),
            _openTileSetButton,
            new ToolStripSeparator(),
            _undoButton,
            _redoButton,
            new ToolStripSeparator(),
            _penButton,
            _selectionButton
        });

        Controls.Add(_editorToolStrip);

        UpdateToolbarCheckedState();
    }

    private void BindTilePalette()
    {
        _tilePalette.TileSelected += index =>
        {
            _selectedTileId = index;
            _mapView.PreviewTileId = index;
            _mapView.Invalidate();
        };
    }

    //========================
    // UI同期
    //========================
    private void UpdateToolbarCheckedState()
    {
        if (_penButton == null || _selectionButton == null)
            return;

        _penButton.Checked = _currentMode == EditorToolMode.Pen;
        _selectionButton.Checked = _currentMode == EditorToolMode.Selection;
    }

    //========================
    // ボタン生成
    //========================
    private static ToolStripButton CreateButton(string name, string tooltip, string glyph, bool checkOnClick = false)
    {
        return new ToolStripButton
        {
            Name = name,
            ToolTipText = tooltip,
            Image = CreateGlyphImage(glyph),
            DisplayStyle = ToolStripItemDisplayStyle.Image,
            CheckOnClick = checkOnClick
        };
    }

    private static ToolStripButton CreateButton(string name, string tooltip, Image image, bool checkOnClick = false)
    {
        return new ToolStripButton
        {
            Name = name,
            ToolTipText = tooltip,
            Image = image,
            DisplayStyle = ToolStripItemDisplayStyle.Image,
            CheckOnClick = checkOnClick
        };
    }

    //========================
    // 仮アイコン
    //========================
    private static Bitmap CreateGlyphImage(string text)
    {
        var bmp = new Bitmap(24, 24);

        using var g = Graphics.FromImage(bmp);
        g.Clear(Color.Transparent);

        using var font = new Font("Yu Gothic UI", 13, FontStyle.Bold, GraphicsUnit.Pixel);
        using var brush = new SolidBrush(Color.Black);

        var size = g.MeasureString(text, font);

        g.DrawString(text, font, brush,
            (24 - size.Width) / 2,
            (24 - size.Height) / 2);

        return bmp;
    }
}
