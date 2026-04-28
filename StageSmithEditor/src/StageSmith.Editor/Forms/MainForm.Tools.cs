using StageSmith.Application.Commands;
using StageSmith.Core.Constants;
using StageSmith.Editor.Tools;
using StageSmith.Infrastructure.Persistence;

namespace StageSmith.Editor;

public partial class MainForm
{
    private PenTool? _penTool;
    private PickerTool? _pickerTool;
    private SelectionTool? _selectionTool;

    private DragPaintCommand? _currentDragCommand;

    private EditorToolMode _currentMode = EditorToolMode.Pen;

    // =========================
    // 初期化
    // =========================

    private void InitializeTools()
    {
        // ===== Pen =====
        _penTool = new PenTool((x, y) =>
        {
            if (_page == null || _selectedTileId < 0)
                return;

            _currentDragCommand?.Add(x, y, (byte)_selectedTileId);

            _mapView.Invalidate();
        });

        // ===== Picker =====
        _pickerTool = new PickerTool(
            (x, y) => _page?.TileMap.GetTile(x, y) ?? -1,
            tileId =>
            {
                if (tileId < 0) return;

                _selectedTileId = tileId;
                _tilePalette.SetSelected(tileId);

                _mapView.PreviewTileId = tileId;
            });

        // ===== Selection =====
        _selectionTool = new SelectionTool(
            (x, y) =>
            {
                var map = _page?.TileMap;
                if (map == null) return 0;
                return (byte)map.GetTile(x, y);
            },
            () => (_page?.TileMap.Width ?? 16, _page?.TileMap.Height ?? 15)
        );
        _selectionTool.SelectionChanged += () => _mapView.Invalidate();
        _selectionTool.MoveRequested += OnSelectionMoveRequested;

        // ===== MapView接続 =====
        //_mapView.CurrentTool = _penTool;
        _mapView.ToolManager = _toolManager;

        _toolManager.SetTool(_penTool);

        _mapView.PickerTool = _pickerTool;
        _mapView.SelectionTool = _selectionTool;

        // ===== Drag Command 制御 =====
        _mapView.MouseDown += (s, e) =>
        {
            if (e.Button == MouseButtons.Left &&
                _page != null &&
                _currentMode == EditorToolMode.Pen)
            {
                _currentDragCommand = new DragPaintCommand(_page.TileMap);
            }
        };

        _mapView.MouseUp += (s, e) =>
        {
            if (_currentDragCommand != null &&
                _currentDragCommand.HasChanges)
            {
                _commandManager.Execute(_currentDragCommand);
                _mapView.Invalidate();
            }

            _currentDragCommand = null;
        };
    }

    private void InitializeToolStrip()
    {
        _editorToolStrip = new ToolStrip
        {
            Dock = DockStyle.Top,
            GripStyle = ToolStripGripStyle.Hidden,
            ImageScalingSize = new Size(24, 24)
        };

        _openButton = CreateToolStripButton("Open", "開く", "📂");
        _saveButton = CreateToolStripButton("Save", "保存", "💾");

        _undoButton = CreateToolStripButton("Undo", "元に戻す", "↶");
        _redoButton = CreateToolStripButton("Redo", "やり直し", "↷");

        _penButton = CreateToolStripButton("Pen", "ペン", "✎", checkOnClick: true);
        _selectionButton = CreateToolStripButton("Selection", "選択", "□", checkOnClick: true);

        _openButton.Click += (_, _) =>
        {
            // TODO: Open処理
        };

        _saveButton.Click += (_, _) =>
        {
            // HACK: とりあえず固定パスで保存。後でファイルダイアログにする。
            SaveProject();
        };

        _undoButton.Click += (_, _) =>
        {
            _commandManager.Undo();
            UpdateToolbarCheckedState();
            _mapView.Invalidate();
        };

        _redoButton.Click += (_, _) =>
        {
            _commandManager.Redo();
            UpdateToolbarCheckedState();
            _mapView.Invalidate();
        };

        _penButton.Click += (_, _) =>
        {
            SetToolMode(EditorToolMode.Pen);
        };

        _selectionButton.Click += (_, _) =>
        {
            SetToolMode(EditorToolMode.Selection);
        };

        _editorToolStrip.Items.AddRange(new ToolStripItem[]
        {
        _openButton,
        _saveButton,
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

    private ToolStripButton CreateToolStripButton(string name, string tooltip, string glyph, bool checkOnClick = false)
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

    private static Bitmap CreateGlyphImage(string text)
    {
        var bmp = new Bitmap(24, 24);

        using var g = Graphics.FromImage(bmp);
        g.Clear(Color.Transparent);

        using var font = new Font("Yu Gothic UI", 13, FontStyle.Bold, GraphicsUnit.Pixel);
        using var brush = new SolidBrush(Color.Black);

        var size = g.MeasureString(text, font);

        g.DrawString(
            text,
            font,
            brush,
            (24 - size.Width) / 2,
            (24 - size.Height) / 2
        );

        return bmp;
    }

    // =========================
    // モード切替
    // =========================

    private void SetToolMode(EditorToolMode mode)
    {
        if (_currentMode == mode)
            return;

        _currentMode = mode;

        switch (mode)
        {
            case EditorToolMode.Pen:
                if (_penTool != null)
                {
                    _toolManager.SetTool(_penTool);
                }
                _selectionTool?.ClearSelection();
                break;

            case EditorToolMode.Selection:
                if (_selectionTool != null)
                {
                    _toolManager.SetTool(_selectionTool);
                }
                break;
        }

        UpdateModeUI();

        _mapView.Invalidate();
    }

    private void UpdateModeUI()
    {
        UpdateToolbarCheckedState();

        // 必要なら後でステータスバーへ表示
        // lblMode.Text = _currentMode.ToString();
    }

    private void UpdateToolbarCheckedState()
    {
        if (_penButton == null || _selectionButton == null)
            return;

        _penButton.Checked = _currentMode == EditorToolMode.Pen;
        _selectionButton.Checked = _currentMode == EditorToolMode.Selection;
    }
}
