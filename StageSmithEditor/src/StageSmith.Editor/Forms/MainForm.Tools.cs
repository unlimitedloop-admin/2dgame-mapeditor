using StageSmith.Application.Commands;
using StageSmith.Core.Constants;
using StageSmith.Editor.Tools;

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
    private ToolStripButton _saveAsButton = null!;
    private ToolStripButton _exportBinButton = null!;
    private ToolStripButton _openTileSetButton = null!;
    private ToolStripButton _undoButton = null!;
    private ToolStripButton _redoButton = null!;
    private ToolStripButton _penButton = null!;
    private ToolStripButton _selectionButton = null!;
    private ToolStripButton _showGridButton = null!;
    private ToolStripButton _tilePreviewButton = null!;
    private ToolStripButton _addPageButton = null!;

    //========================
    // ツールストリップ初期化
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
        _saveAsButton = CreateButton("SaveAs", "名前を付けて保存", StageSmithEditor.Properties.Resources.icons8_名前を付けて保存_30);

        _exportBinButton = CreateButton("ExportBin", "バイナリ出力", StageSmithEditor.Properties.Resources.icons8_バイナリファイル_30);
        _openTileSetButton = CreateButton("OpenTileSet", "タイル画像を開く", StageSmithEditor.Properties.Resources.icons8_画像を開く_30);

        _undoButton = CreateButton("Undo", "元に戻す (Ctrl+Z)", StageSmithEditor.Properties.Resources.icons8_元に戻す_30);
        _redoButton = CreateButton("Redo", "やり直し (Ctrl+Y)", StageSmithEditor.Properties.Resources.icons8_やり直す_30);

        _penButton = CreateButton("Pen", "ペン (P)", StageSmithEditor.Properties.Resources.icons8_鉛筆_24, true);
        _selectionButton = CreateButton("Selection", "選択 (S)", StageSmithEditor.Properties.Resources.icons8_選択_24, true);
        _showGridButton = CreateButton("ShowGrid", "グリッド表示切替 (G)", StageSmithEditor.Properties.Resources.icons8_グリッド_24, true);
        _tilePreviewButton = CreateButton("TilePreview", "タイルプレビュー切替 (T)", StageSmithEditor.Properties.Resources.icons8_目に見える_24, true);
        _addPageButton = CreateButton("AddPage", "ページを追加 (Ctrl+T)", StageSmithEditor.Properties.Resources.icons8_ファイル追加_30, true);

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

        _saveAsButton.Click += (_, _) =>
        {
            SaveProjectAs();
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

        _showGridButton.Click += (_, _) =>
        {
            ShowMapViewGrid();
        };

        _tilePreviewButton.Click += (_, _) =>
        {
            _mapView.ShowPreview = !_mapView.ShowPreview;
            UpdateTilePreviewIcon();
        };

        _addPageButton.Click += (_, _) =>
        {
            AddPageToCurrentStage();
        };

        //========================
        // UI構築
        //========================
        _editorToolStrip.Items.AddRange(
        [
            _newButton,
            _openButton,
            _saveButton,
            _saveAsButton,
            new ToolStripSeparator(),
            _openTileSetButton,
            new ToolStripSeparator(),
            _exportBinButton,
            new ToolStripSeparator(),
            _undoButton,
            _redoButton,
            new ToolStripSeparator(),
            _penButton,
            _selectionButton,
            _showGridButton,
            _tilePreviewButton,
            new ToolStripSeparator(),
            _addPageButton
        ]);

        Controls.Add(_editorToolStrip);

        UpdateToolbarCheckedState();
    }

    // =========================
    // ツール初期化
    // =========================
    private void InitializeTools()
    {
        // Pen
        _penTool = new PenTool(
            () => _page?.TileMap,
            () => _selectedTileId,
            _commandManager,
            () => _mapView.Invalidate()
        );

        // Picker
        _pickerTool = new PickerTool(
            (x, y) => _page?.TileMap.GetTile(x, y) ?? -1,
            tileId =>
            {
                if (tileId < 0) return;

                _selectedTileId = tileId;
                _tilePalette.SetSelected(tileId);
                _mapView.PreviewTileId = tileId;
            }
        );

        // Selection
        _selectionTool = new SelectionTool(
            (x, y) =>
            {
                var map = _page?.TileMap;
                return (byte)(map == null ? 0 : map.GetTile(x, y));
            },
            () => (_page?.TileMap.Width ?? 16, _page?.TileMap.Height ?? 15)
        );

        _selectionTool.SelectionChanged += () => _mapView.Invalidate();
        _selectionTool.MoveRequested += OnSelectionMoveRequested;
        _selectionTool.Confirmed += ApplySelectionFill;

        // Fill
        _fillTool = new FillTool(
            () => _page?.TileMap,
            positions =>
            {
                if (_page == null || _selectedTileId < 0)
                    return;

                var command = new TilePaintCommand(
                    _page.TileMap,
                    positions,
                    (byte)_selectedTileId
                );

                _commandManager.Execute(command);
                _mapView.Invalidate();
            },
            _selectedTileId
        );

        // MapView接続（DockContent 生成後なので直接参照可能）
        _mapView.ToolManager = _toolManager;
        _mapView.PickerTool = _pickerTool;
        _mapView.SelectionTool = _selectionTool;
        _mapView.FillTool = _fillTool;

        _toolManager.SetTool(_penTool);

        // Drag Command
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
            if (_currentDragCommand != null && _currentDragCommand.HasChanges)
            {
                _commandManager.Execute(_currentDragCommand);
                _mapView.Invalidate();

                // ノードエディタが開いていれば現在ページのプレビューを更新する
                if (_page != null)
                    _nodeEditorForm?.InvalidatePagePreview(_page.Id, _page);
            }

            _currentDragCommand = null;
        };
    }

    //========================
    // ToolManager 同期
    //========================
    private void BindToolManager()
    {
        _toolManager.ToolChanged += tool =>
        {
            if (ReferenceEquals(tool, _penTool))
                _currentMode = EditorToolMode.Pen;
            else if (ReferenceEquals(tool, _selectionTool))
                _currentMode = EditorToolMode.Selection;

            UpdateToolbarCheckedState();
            _mapView.Invalidate();
        };
    }

    //========================
    // Tool 変更（Command 経由）
    //========================
    private void ChangeTool(ITool? tool)
    {
        if (tool == null) return;

        _commandManager.Execute(
            new ChangeToolCommand(_toolManager, tool)
        );
    }

    //========================
    // モード変更（統一）
    //========================
    private void SetToolMode(EditorToolMode mode)
    {
        if (_currentMode == mode)
            return;

        ITool? tool = mode switch
        {
            EditorToolMode.Pen => _penTool,
            EditorToolMode.Selection => _selectionTool,
            _ => null
        };

        if (tool == null)
            return;

        ChangeTool(tool);

        if (mode == EditorToolMode.Pen)
            _selectionTool?.ClearSelection();
    }

    private void BindTilePalette()
    {
        _tilePalette.TileSelected += index =>
        {
            _selectedTileId = index;
            _mapView.PreviewTileId = index;
            _mapView.Invalidate();
        };

        _tilePalette.TilesetImageSelectionRequested += (_, _) =>
        {
            OpenTilesetImage();
        };
    }

    //========================
    // MetaTilePalette バインド
    //========================
    private void BindMetaTilePalette()
    {
        _metaTilePalette.MetaTileSelected += metaTile =>
        {
            _context.SetSelectedMetaTile(metaTile);

            // 通常タイル選択と競合しないように、見た目上の選択を解除する。
            _selectedTileId = -1;
            _tilePalette.SetSelected(-1);

            // 現段階では MapView 側の配置処理は次フェーズ。
            // プレビューも通常タイル用なので一旦消す。
            _mapView.PreviewTileId = -1;
            _mapView.ShowPreview = false;
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
