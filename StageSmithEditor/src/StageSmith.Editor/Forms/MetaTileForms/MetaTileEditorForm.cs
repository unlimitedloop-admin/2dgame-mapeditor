using StageSmith.Application.Commands;
using StageSmith.Core.Models;
using StageSmith.Editor.Controls;

namespace StageSmith.Editor;

public sealed class MetaTileEditorForm : Form
{
    private readonly Stage _stage;

    /// <summary>
    /// 直接 Stage.MetaTiles を編集せず、作業コピーを編集する。
    /// Save / Save As 時に Stage へ反映する。
    /// </summary>
    private MetaTile _currentMetaTile = new(4, 4);

    private readonly TilePaletteControl _tilePalette = new();
    private readonly MetaTileCanvasControl _canvas = new();
    private readonly MetaTilePreviewControl _preview = new();

    private readonly ComboBox _savedMetaTileComboBox = new();
    private readonly TextBox _nameTextBox = new();
    private readonly ComboBox _sizeComboBox = new();
    private readonly Label _statusLabel = new();

    private CommandManager _commandManager = new();

    private readonly Dictionary<(int X, int Y), MetaTileCellChange> _pendingPaintChanges = [];
    private bool _isPainting;

    private bool _suppressSizeChanged;
    private bool _suppressSavedSelectionChanged;

    /// <summary>
    /// null の場合は未保存の新規メタタイル。
    /// 値がある場合は、その Id の保存済みメタタイルを改訂中。
    /// </summary>
    private int? _editingMetaTileId;

    private int _selectedTileId = -1;

    public MetaTileEditorForm(Stage stage, Bitmap? tileset)
    {
        _stage = stage ?? throw new ArgumentNullException(nameof(stage));

        Text = "MetaTile Editor";
        StartPosition = FormStartPosition.CenterParent;
        Size = new Size(820, 560);

        InitializeLayout();
        InitializeCommandManager();

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

        RefreshSavedMetaTileList();
        SetWorkingMetaTile(new MetaTile(4, 4), editingMetaTileId: null);
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

        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 72));
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
            WrapContents = true,
            Padding = new Padding(8, 6, 8, 4)
        };

        var newButton = new Button
        {
            Text = "New",
            AutoSize = true
        };

        newButton.Click += (_, _) =>
        {
            SetWorkingMetaTile(new MetaTile(4, 4), editingMetaTileId: null);
        };

        var saveButton = new Button
        {
            Text = "Save",
            AutoSize = true
        };

        saveButton.Click += (_, _) => SaveCurrentMetaTile();

        var saveAsButton = new Button
        {
            Text = "Save As",
            AutoSize = true
        };

        saveAsButton.Click += (_, _) => SaveCurrentMetaTileAsNew();

        var deleteButton = new Button
        {
            Text = "Delete",
            AutoSize = true
        };

        deleteButton.Click += (_, _) => DeleteCurrentMetaTile();

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

        _savedMetaTileComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        _savedMetaTileComboBox.Width = 180;
        _savedMetaTileComboBox.SelectedIndexChanged += (_, _) =>
        {
            if (_suppressSavedSelectionChanged)
                return;

            if (_savedMetaTileComboBox.SelectedItem is not MetaTileListItem item)
                return;

            LoadSavedMetaTile(item.Id);
        };

        _nameTextBox.Width = 150;
        _nameTextBox.TextChanged += (_, _) =>
        {
            _currentMetaTile.Name = GetMetaTileName();
        };

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

        _sizeComboBox.SelectedIndexChanged += (_, _) =>
        {
            if (_suppressSizeChanged)
                return;

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
            RefreshMetaTileViews();
        };

        _statusLabel.AutoSize = true;
        _statusLabel.Padding = new Padding(12, 5, 0, 0);

        panel.Controls.Add(new Label
        {
            Text = "Saved:",
            AutoSize = true,
            Padding = new Padding(0, 5, 4, 0)
        });
        panel.Controls.Add(_savedMetaTileComboBox);

        panel.Controls.Add(new Label
        {
            Text = "Name:",
            AutoSize = true,
            Padding = new Padding(8, 5, 4, 0)
        });
        panel.Controls.Add(_nameTextBox);

        panel.Controls.Add(new Label
        {
            Text = "Size:",
            AutoSize = true,
            Padding = new Padding(8, 5, 4, 0)
        });
        panel.Controls.Add(_sizeComboBox);

        panel.Controls.Add(newButton);
        panel.Controls.Add(saveButton);
        panel.Controls.Add(saveAsButton);
        panel.Controls.Add(deleteButton);
        panel.Controls.Add(clearButton);
        panel.Controls.Add(_statusLabel);

        return panel;
    }

    private void InitializeCommandManager()
    {
        _commandManager.HistoryChanged += RefreshMetaTileViews;
    }

    private void ResetCommandManager()
    {
        _commandManager.HistoryChanged -= RefreshMetaTileViews;
        _commandManager = new CommandManager();
        _commandManager.HistoryChanged += RefreshMetaTileViews;
    }

    private void SetWorkingMetaTile(MetaTile metaTile, int? editingMetaTileId)
    {
        _editingMetaTileId = editingMetaTileId;
        _currentMetaTile = metaTile.Clone(keepId: editingMetaTileId.HasValue);

        if (editingMetaTileId.HasValue)
        {
            _currentMetaTile.Id = editingMetaTileId.Value;
        }

        _nameTextBox.Text = _currentMetaTile.Name;
        SetSizeComboBoxFromMetaTile(_currentMetaTile);

        _canvas.SetMetaTile(_currentMetaTile);
        _preview.SetMetaTile(_currentMetaTile);

        ResetCommandManager();
        RefreshMetaTileViews();
        UpdateStatus();
    }

    private void SetSizeComboBoxFromMetaTile(MetaTile metaTile)
    {
        _suppressSizeChanged = true;

        var sizeText = $"{metaTile.Width} x {metaTile.Height}";
        var index = _sizeComboBox.Items.IndexOf(sizeText);

        _sizeComboBox.SelectedIndex = index >= 0
            ? index
            : _sizeComboBox.Items.IndexOf("4 x 4");

        _suppressSizeChanged = false;
    }

    private string GetMetaTileName()
    {
        var name = _nameTextBox.Text.Trim();
        return string.IsNullOrWhiteSpace(name)
            ? "New MetaTile"
            : name;
    }

    private void SaveCurrentMetaTile()
    {
        _currentMetaTile.Name = GetMetaTileName();

        if (_editingMetaTileId.HasValue)
        {
            _currentMetaTile.Id = _editingMetaTileId.Value;

            if (_stage.UpdateMetaTile(_currentMetaTile))
            {
                RefreshSavedMetaTileList(selectId: _editingMetaTileId);
                UpdateStatus("Saved.");
                return;
            }
        }

        var saved = _stage.AddMetaTile(_currentMetaTile);
        _editingMetaTileId = saved.Id;
        SetWorkingMetaTile(saved, saved.Id);
        RefreshSavedMetaTileList(selectId: saved.Id);
        UpdateStatus("Saved as new.");
    }

    private void SaveCurrentMetaTileAsNew()
    {
        _currentMetaTile.Name = GetMetaTileName();

        var saved = _stage.AddMetaTile(_currentMetaTile);
        _editingMetaTileId = saved.Id;

        SetWorkingMetaTile(saved, saved.Id);
        RefreshSavedMetaTileList(selectId: saved.Id);
        UpdateStatus("Saved as new.");
    }

    private void DeleteCurrentMetaTile()
    {
        if (!_editingMetaTileId.HasValue)
        {
            UpdateStatus("Nothing to delete.");
            return;
        }

        var metaTileId = _editingMetaTileId.Value;
        var target = _stage.FindMetaTile(metaTileId);

        if (target is null)
        {
            UpdateStatus("Selected MetaTile was not found.");
            RefreshSavedMetaTileList();
            SetWorkingMetaTile(new MetaTile(4, 4), editingMetaTileId: null);
            return;
        }

        var result = MessageBox.Show(
            this,
            $"Delete MetaTile {target.Id:D3}: {target.Name}?",
            "Delete MetaTile",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning
        );

        if (result != DialogResult.Yes)
            return;

        if (!_stage.RemoveMetaTile(metaTileId))
        {
            UpdateStatus("Delete failed.");
            RefreshSavedMetaTileList();
            return;
        }

        RefreshSavedMetaTileList();
        SetWorkingMetaTile(new MetaTile(4, 4), editingMetaTileId: null);
        UpdateStatus("Deleted.");
    }

    private void LoadSavedMetaTile(int metaTileId)
    {
        var saved = _stage.FindMetaTile(metaTileId);
        if (saved is null)
        {
            UpdateStatus("Selected MetaTile was not found.");
            RefreshSavedMetaTileList();
            return;
        }

        SetWorkingMetaTile(saved, saved.Id);
        UpdateStatus("Loaded.");
    }

    private void RefreshSavedMetaTileList(int? selectId = null)
    {
        _suppressSavedSelectionChanged = true;

        _savedMetaTileComboBox.Items.Clear();

        foreach (var metaTile in _stage.MetaTiles.OrderBy(x => x.Id))
        {
            _savedMetaTileComboBox.Items.Add(new MetaTileListItem(metaTile.Id, metaTile.Name));
        }

        if (selectId.HasValue)
        {
            for (var i = 0; i < _savedMetaTileComboBox.Items.Count; i++)
            {
                if (_savedMetaTileComboBox.Items[i] is MetaTileListItem item &&
                    item.Id == selectId.Value)
                {
                    _savedMetaTileComboBox.SelectedIndex = i;
                    break;
                }
            }
        }
        else
        {
            _savedMetaTileComboBox.SelectedIndex = -1;
        }

        _suppressSavedSelectionChanged = false;
    }

    private void UpdateStatus(string? message = null)
    {
        var mode = _editingMetaTileId.HasValue
            ? $"Editing ID {_editingMetaTileId.Value:D3}"
            : "New / Unsaved";

        _statusLabel.Text = string.IsNullOrWhiteSpace(message)
            ? mode
            : $"{mode} - {message}";
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

        if (keyData == (Keys.Control | Keys.A))
        {
            var focusedControl = FindFocusedControl(this);

            if (focusedControl is TextBoxBase textBox)
            {
                textBox.SelectAll();
                return true;
            }

            // ComboBox などに Ctrl+A が流れると例外になる場合があるため、現時点では握りつぶす。
            return true;
        }

        return base.ProcessCmdKey(ref msg, keyData);
    }

    private static Control? FindFocusedControl(Control parent)
    {
        // TODO: Implement logic to find the focused control within the form.
        foreach(Control child in parent.Controls)
        {
            if (child.Focused)
            {
                return child;
            }

            var focusedChild = FindFocusedControl(child);
            if (focusedChild != null)
            {
                return focusedChild;
            }
        }

        return null;
    }

    private sealed class MetaTileListItem
    {
        public int Id { get; }
        private readonly string _name;

        public MetaTileListItem(int id, string name)
        {
            Id = id;
            _name = name;
        }

        public override string ToString()
        {
            return $"{Id:D3}: {_name}";
        }
    }
}
