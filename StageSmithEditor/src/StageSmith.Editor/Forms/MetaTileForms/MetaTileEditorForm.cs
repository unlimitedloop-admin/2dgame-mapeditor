using StageSmith.Application.Commands;
using StageSmith.Core.Models;
using StageSmith.Editor.Controls;

namespace StageSmith.Editor;

public sealed class MetaTileEditorForm : Form
{
    private readonly MetaTile _currentMetaTile = new(4, 4);

    private readonly TilePaletteControl _tilePalette = new();
    private readonly MetaTileCanvasControl _canvas = new();
    private readonly MetaTilePreviewControl _preview = new();

    private readonly ComboBox _sizeComboBox = new();

    private readonly CommandManager _commandManager = new();

    private readonly Dictionary<(int X, int Y), MetaTileCellChange> _pendingPaintChanges = [];
    private bool _isPainting;

    private int _selectedTileId = -1;

    public MetaTileEditorForm(Bitmap? tileset)
    {
        Text = "MetaTile Editor";
        StartPosition = FormStartPosition.CenterParent;
        Size = new Size(720, 520);

        InitializeLayout();

        _tilePalette.SetTileset(tileset);
        _canvas.SetTileset(tileset);
        _preview.SetTileset(tileset);

        _canvas.SetMetaTile(_currentMetaTile);
        _preview.SetMetaTile(_currentMetaTile);

        _tilePalette.TileSelected += tileId =>
        {
            _selectedTileId = tileId;
            _canvas.SetSelectedTile(tileId);
        };

        _canvas.TilePicked += tileId =>
        {
            _selectedTileId = tileId;
            _tilePalette.SetSelected(tileId);
            _canvas.SetSelectedTile(tileId);
        };

        _canvas.EditStarted += BeginMetaTilePaint;
        _canvas.TilePaintRequested += PaintMetaTileCell;
        _canvas.EditFinished += EndMetaTilePaint;

        _commandManager.HistoryChanged += RefreshMetaTileViews;
    }

    private void InitializeLayout()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 3
        };

        root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 240));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 70));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 30));

        var toolBar = CreateToolBar();

        var paletteGroup = new GroupBox
        {
            Text = "Tile Palette",
            Dock = DockStyle.Fill
        };

        _tilePalette.Dock = DockStyle.Fill;
        paletteGroup.Controls.Add(_tilePalette);

        var canvasGroup = new GroupBox
        {
            Text = "Edit Canvas",
            Dock = DockStyle.Fill
        };

        var canvasHost = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            BackColor = Color.FromArgb(24, 24, 24)
        };

        canvasHost.Controls.Add(_canvas);
        canvasGroup.Controls.Add(canvasHost);

        var previewGroup = new GroupBox
        {
            Text = "Preview",
            Dock = DockStyle.Fill
        };

        _preview.Dock = DockStyle.Fill;
        previewGroup.Controls.Add(_preview);

        root.Controls.Add(toolBar, 0, 0);
        root.SetColumnSpan(toolBar, 2);

        root.Controls.Add(paletteGroup, 0, 1);
        root.Controls.Add(canvasGroup, 1, 1);

        root.Controls.Add(previewGroup, 0, 2);
        root.SetColumnSpan(previewGroup, 2);

        Controls.Add(root);
    }

    private FlowLayoutPanel CreateToolBar()
    {
        var panel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            Padding = new Padding(8, 6, 8, 4)
        };

        panel.Controls.Add(new Label
        {
            Text = "Size:",
            AutoSize = true,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(0, 5, 4, 0)
        });

        _sizeComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        _sizeComboBox.Width = 100;
        _sizeComboBox.Items.AddRange(
        [
            "2 x 2",
            "4 x 4",
            "8 x 8",
            "1 x 4",
            "4 x 1"
        ]);
        _sizeComboBox.SelectedIndex = 1;

        _sizeComboBox.SelectedIndexChanged += (_, _) =>
        {
            var (w, h) = _sizeComboBox.SelectedItem?.ToString() switch
            {
                "2 x 2" => (2, 2),
                "4 x 4" => (4, 4),
                "8 x 8" => (8, 8),
                "1 x 4" => (1, 4),
                "4 x 1" => (4, 1),
                _ => (4, 4)
            };

            _currentMetaTile.Resize(w, h);
            _canvas.SetMetaTile(_currentMetaTile);
            _preview.SetMetaTile(_currentMetaTile);
        };

        var clearButton = new Button
        {
            Text = "Clear",
            AutoSize = true
        };

        clearButton.Click += (_, _) =>
        {
            _commandManager.Execute(
                new MetaTileClearCommand(_currentMetaTile, RefreshMetaTileViews)
            );
        };

        panel.Controls.Add(_sizeComboBox);
        panel.Controls.Add(clearButton);

        return panel;
    }

    private void BeginMetaTilePaint()
    {
        _isPainting = true;
        _pendingPaintChanges.Clear();
    }

    private void PaintMetaTileCell(int x, int y, byte tileId)
    {
        if (!_isPainting)
            return;

        var beforeTileId = _currentMetaTile.GetTile(x, y);

        if (!_pendingPaintChanges.TryGetValue((x, y), out var existingChange))
        {
            if (beforeTileId == tileId)
                return;

            _pendingPaintChanges[(x, y)] = new MetaTileCellChange(
                x,
                y,
                beforeTileId,
                tileId
            );
        }
        else
        {
            if (existingChange.BeforeTileId == tileId)
            {
                _pendingPaintChanges.Remove((x, y));
            }
            else
            {
                _pendingPaintChanges[(x, y)] = existingChange with
                {
                    AfterTileId = tileId
                };
            }
        }

        _currentMetaTile.SetTile(x, y, tileId);
        RefreshMetaTileViews();
    }

    private void EndMetaTilePaint()
    {
        if (!_isPainting)
            return;

        _isPainting = false;

        if (_pendingPaintChanges.Count == 0)
            return;

        var changes = _pendingPaintChanges.Values.ToList();
        _pendingPaintChanges.Clear();

        var command = new MetaTileBatchPaintCommand(
            _currentMetaTile,
            changes,
            RefreshMetaTileViews
        );

        _commandManager.Execute(command);
    }

    private void RefreshMetaTileViews()
    {
        _canvas.Invalidate();
        _preview.Invalidate();
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (keyData == (Keys.Control | Keys.Z))
        {
            _commandManager.Undo();
            return true;
        }

        if (keyData == (Keys.Control | Keys.Y))
        {
            _commandManager.Redo();
            return true;
        }

        return base.ProcessCmdKey(ref msg, keyData);
    }
}
