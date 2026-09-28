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
    private ToolStripButton _bucketButton = null!;
    private ToolStripButton _markerButton = null!;
    private ToolStripButton _objectButton = null!;
    private ToolStripButton _showGridButton = null!;
    private ToolStripButton _tilePreviewButton = null!;
    private ToolStripButton _numberLabelButton = null!;
    private ToolStripButton _addPageButton = null!;
    private ToolStripButton _removePageButton = null!;
    private ToolStripButton _tileSearchButton = null!;
    private ToolStripButton _tileReplaceButton = null!;
    private ToolStripButton _tileSearchPrevHitButton = null!;
    private ToolStripButton _tileSearchNextHitButton = null!;
    private ToolStripButton _tileSearchClearButton = null!;

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
        _bucketButton = CreateButton("Bucket", "バケツ (B)", StageSmithEditor.Properties.Resources.icons8_バケツ_24, true);
        _markerButton = CreateButton("Marker", "マーカー (K)", StageSmithEditor.Properties.Resources.icons8_マーカー_24, true);
        _objectButton = CreateButton("Object", "オブジェクト配置 (O)", StageSmithEditor.Properties.Resources.icons8_材料_30, true);

        _showGridButton = CreateButton("ShowGrid", "グリッド表示切替 (G)", StageSmithEditor.Properties.Resources.icons8_グリッド_24, true);
        _tilePreviewButton = CreateButton("TilePreview", "タイルプレビュー切替 (T)", StageSmithEditor.Properties.Resources.icons8_目に見える_24, true);
        _numberLabelButton = CreateButton("NumberLabel", "番号ラベル切替 (L)", StageSmithEditor.Properties.Resources.icons8_数字_30, true);

        _addPageButton = CreateButton("AddPage", "ページを追加 (Ctrl+T)", StageSmithEditor.Properties.Resources.icons8_ファイル追加_30);
        _removePageButton = CreateButton("RemovePage", "ページを削除 (Ctrl+Shift+T)", StageSmithEditor.Properties.Resources.icons8_delete_file_30);

        _tileSearchButton = CreateButton("TileSearch", "タイル検索 (Ctrl+F)", StageSmithEditor.Properties.Resources.icons8_検索_30);
        _tileReplaceButton = CreateButton("TileReplace", "タイル置換 (Ctrl+H)", StageSmithEditor.Properties.Resources.icons8_置換_30);
        _tileSearchPrevHitButton = CreateButton("TileSearchPrevHit", "前の検索結果 (Shift+F4)", StageSmithEditor.Properties.Resources.ai_前を検索_40);
        _tileSearchNextHitButton = CreateButton("TileSearchNextHit", "次の検索結果 (F4)", StageSmithEditor.Properties.Resources.ai_次を検索_40);
        _tileSearchClearButton = CreateButton("TileSearchClear", "検索結果をクリア (Ctrl+Shift+F)", StageSmithEditor.Properties.Resources.ai_検索結果を削除_40);

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

        _bucketButton.Click += (_, _) =>
        {
            if (_fillTool != null)
                ChangeTool(_fillTool);
        };

        _markerButton.Click += (_, _) =>
        {
            if (_markerTool != null)
                ChangeTool(_markerTool);
        };

        _objectButton.Click += (_, _) =>
        {
            if (_objectTool != null)
                ChangeTool(_objectTool);
        };

        _openTileSetButton.Click += (_, _) =>
        {
            OpenTilesetImage();
        };

        _showGridButton.Click += (_, _) =>
        {
            _menuViewGridLines.Checked = !_menuViewGridLines.Checked;
        };

        _tilePreviewButton.Click += (_, _) =>
        {
            _menuViewTilePreview.Checked = !_menuViewTilePreview.Checked;
        };

        _numberLabelButton.Click += (_, _) =>
        {
            _menuViewShowTileNumbers.Checked = !_menuViewShowTileNumbers.Checked;
        };

        _addPageButton.Click += (_, _) =>
        {
            AddPageToCurrentStage();
        };

        _removePageButton.Click += (_, _) =>
        {
            RemoveCurrentPage();
        };

        _tileSearchButton.Click += (_, _) =>
        {
            OpenFindTileDialog();
        };

        _tileReplaceButton.Click += (_, _) =>
        {
            OpenReplaceTileDialog();
        };

        _tileSearchPrevHitButton.Click += (_, _) =>
        {
            FindPrevTile();
        };

        _tileSearchNextHitButton.Click += (_, _) =>
        {
            FindNextTile();
        };

        _tileSearchClearButton.Click += (_, _) =>
        {
            ClearSearchHighlight();
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
            _bucketButton,
            _markerButton,
            _objectButton,
            new ToolStripSeparator(),
            _showGridButton,
            _tilePreviewButton,
            _numberLabelButton,
            new ToolStripSeparator(),
            _addPageButton,
            _removePageButton,
            new ToolStripSeparator(),
            _tileSearchButton,
            _tileReplaceButton,
            _tileSearchPrevHitButton,
            _tileSearchNextHitButton,
            _tileSearchClearButton
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
                _context.SetSelectedTile(tileId);
                _tilePalette.SetSelected(tileId);
                _metaTilePalette.SetSelected(null);
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

        // Marker
        _markerTool = new MarkerTool(
            _markerState,
            () => _context.CurrentPageIndex,
            () => _context.CurrentStage?.Id ?? Guid.Empty,
            () => _mapView.Invalidate()
        );

        _markerState.Changed += () => _mapView.Invalidate();

        // Object（敵などのエンティティ配置）
        InitializeObjectTool();

        // MapView接続（DockContent 生成後なので直接参照可能）
        _mapView.ToolManager = _toolManager;
        _mapView.PickerTool = _pickerTool;
        _mapView.SelectionTool = _selectionTool;
        _mapView.FillTool = _fillTool;
        _mapView.MarkerTool = _markerTool;
        _mapView.MarkerState = _markerState;
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
            else if (ReferenceEquals(tool, _fillTool))
                _currentMode = EditorToolMode.Bucket;
            else if (ReferenceEquals(tool, _markerTool))
                _currentMode = EditorToolMode.Marker;
            else if (ReferenceEquals(tool, _objectTool))
                _currentMode = EditorToolMode.Object;

            // ツールバー/メニュー/ショートカットキーいずれの経路で切り替わっても
            // 必ずこのイベントを通るため、モード変更に伴う副作用はここに集約する。
            if (_currentMode != EditorToolMode.Selection)
                _selectionTool?.ClearSelection();

            if (_currentMode != EditorToolMode.Object)
            {
                _objectTool?.ClearSelection();
                _objectPalette.SetPlayerStartMode(false);
            }

            // Objectツールに切り替えたら、オブジェクト表示を自動的にONにする（Markerと同じ流儀）
            if (_currentMode == EditorToolMode.Object && !_menuViewShowEntities.Checked)
                _menuViewShowEntities.Checked = true;

            // Markerツールに切り替えたら、マーカーオーバーレイを自動的に表示する
            if (_currentMode == EditorToolMode.Marker && !_menuViewMarkerOverlay.Checked)
                _menuViewMarkerOverlay.Checked = true;

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

        _toolManager.SetTool(tool);
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
            EditorToolMode.Marker => _markerTool,
            EditorToolMode.Object => _objectTool,
            EditorToolMode.Bucket => _fillTool,
            _ => null
        };

        if (tool == null)
            return;

        ChangeTool(tool);
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

            _selectedTileId = -1;
            _tilePalette.SetSelected(-1);

            _mapView.PreviewTileId = -1;
            // _mapView.ShowPreview = false;  ← 削除
            _mapView.Invalidate();
        };

        _metaTilePalette.MetaTileEditorRequested += () =>
        {
            OpenMetaTileEditor();
        };
    }

    //========================
    // UI同期
    //========================
    private void UpdateToolbarCheckedState()
    {
        if (_penButton == null || _selectionButton == null || _bucketButton == null || _markerButton == null || _objectButton == null)
            return;

        _penButton.Checked = _currentMode == EditorToolMode.Pen;
        _selectionButton.Checked = _currentMode == EditorToolMode.Selection;
        _bucketButton.Checked = _currentMode == EditorToolMode.Bucket;
        _markerButton.Checked = _currentMode == EditorToolMode.Marker;
        _objectButton.Checked = _currentMode == EditorToolMode.Object;

        _menuToolsPen.Checked = _currentMode == EditorToolMode.Pen;
        _menuToolsSelection.Checked = _currentMode == EditorToolMode.Selection;
        _menuToolsBucket.Checked = _currentMode == EditorToolMode.Bucket;
        _menuToolsMarker.Checked = _currentMode == EditorToolMode.Marker;
        _menuToolsObject.Checked = _currentMode == EditorToolMode.Object;
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
