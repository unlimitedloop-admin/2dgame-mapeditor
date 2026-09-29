using System.Diagnostics;
using StageSmith.Core.Constants;
using StageSmith.Editor.Controls;
using WeifenLuo.WinFormsUI.Docking;

namespace StageSmith.Editor;

public partial class MainForm
{
    private void InitializeMenuHandlers()
    {
        // ========================
        // File
        // ========================
        _menuFileNewProject.Click    += (_, _) => NewProject();
        _menuFileOpenProject.Click   += (_, _) => OpenProject();
        _menuFileSaveProject.Click   += (_, _) => SaveProject();
        _menuFileSaveProjectAs.Click += (_, _) => SaveProjectAs();
        _menuFileCloseProject.Click  += (_, _) => CloseProject();
        _menuFileNewStage.Click      += (_, _) => NewStage();
        _menuFileImportStage.Click   += (_, _) => ImportStage();
        _menuFileSaveStage.Click     += (_, _) => SaveStage();
        _menuFileReloadStage.Click   += (_, _) => ReloadStage();
        _menuFileDropStage.Click     += (_, _) => DropStage();
        _menuFileAddPage.Click       += (_, _) => AddPageToCurrentStage();
        _menuFileDuplicatePage.Click += (_, _) => DuplicateCurrentPage();
        _menuFileRemovePage.Click    += (_, _) => RemoveCurrentPage();
        _menuFileImportTileSet.Click += (_, _) => ImportTileSet();
        _menuFileExportBin.Click     += (_, _) => ExportBin();
        _menuFileExportAll.Click     += (_, _) => ExportAllStages();
        _menuFileQuit.Click          += (_, _) => Close();

        // ========================
        // Edit
        // ========================
        _menuEditUndo.Click                 += (_, _) => _commandManager.Undo();
        _menuEditRedo.Click                 += (_, _) => _commandManager.Redo();
        _menuEditCut.Click                  += (_, _) => CutSelection();
        _menuEditCopy.Click                 += (_, _) => CopySelection();
        _menuEditPaste.Click                += (_, _) => PasteSelection();
        _menuEditDelete.Click               += (_, _) => DeleteSelection();
        _menuEditFill.Click                 += (_, _) => FillSelection();
        _menuEditSelectAllSameTile.Click    += (_, _) => SelectAllSameTile();
        _menuEditSelectEmptyTile.Click      += (_, _) => SelectEmptyTile();
        _menuEditInvertSelection.Click      += (_, _) => InvertSelection();
        _menuEditClearSelection.Click       += (_, _) => ClearSelection();
        _menuEditFindTile.Click             += (_, _) => OpenFindTileDialog();
        _menuEditFindTileOnMap.Click        += (_, _) => FindTileOnMap();
        _menuEditFindNextTile.Click         += (_, _) => FindNextTile();
        _menuEditFindPrevTile.Click         += (_, _) => FindPrevTile();
        _menuEditReplaceTile.Click          += (_, _) => OpenReplaceTileDialog();
        _menuEditClearSearchHighlight.Click += (_, _) => ClearSearchHighlight();
        _menuEditToggleBookmark.Click       += (_, _) => ToggleBookmarkForCurrentPage();
        _menuEditClearAllBookmarks.Click    += (_, _) => ClearAllBookmarks();
        _menuEditClearPageMarkers.Click     += (_, _) => ClearPageMarkers();
        _menuEditClearStageMarkers.Click    += (_, _) => ClearStageMarkers();
        _menuEditClearAllMarkers.Click      += (_, _) => ClearAllMarkers();
        _menuEditToggleReadOnly.CheckedChanged += (_, _) => ApplyReadOnlyState(_menuEditToggleReadOnly.Checked);

        // ========================
        // View
        // ========================
        _menuViewGridLines.CheckedChanged       += (_, _) => ApplyGridState(_menuViewGridLines.Checked);
        _menuViewTilePreview.CheckedChanged     += (_, _) => ApplyTilePreviewState(_menuViewTilePreview.Checked);
        _menuViewShowTileNumbers.CheckedChanged += (_, _) => ToggleShowTileNumbers(_menuViewShowTileNumbers.Checked);
        _menuViewRowNumbers.CheckedChanged      += (_, _) => ApplyRowNumberState(_menuViewRowNumbers.Checked);
        _menuViewColumnNumbers.CheckedChanged   += (_, _) => ApplyColumnNumberState(_menuViewColumnNumbers.Checked);
        _menuViewMarkerOverlay.CheckedChanged   += (_, _) => ToggleMarkerOverlay(_menuViewMarkerOverlay.Checked);
        _menuViewShowEntities.CheckedChanged    += (_, _) => _mapView.SetShowEntities(_menuViewShowEntities.Checked);
        _menuViewTileInfo.CheckedChanged        += (_, _) => _mapView.SetShowTileInfo(_menuViewTileInfo.Checked);
        _menuViewZoomIn.Click                   += (_, _) => ZoomIn();
        _menuViewZoomOut.Click                  += (_, _) => ZoomOut();
        _menuViewResetZoom.Click                += (_, _) => ResetZoom();
        _menuViewToolBar.CheckedChanged         += (_, _) => _editorToolStrip.Visible = _menuViewToolBar.Checked;
        _menuViewStatusBar.CheckedChanged       += (_, _) => _statusStrip.Visible = _menuViewStatusBar.Checked;

        // ========================
        // Navigation
        // ========================
        _menuNavNextPage.Click    += (_, _) => NavigatePage(NavAction.Next);
        _menuNavPrevPage.Click    += (_, _) => NavigatePage(NavAction.Prev);
        _menuNavFirstPage.Click   += (_, _) => NavigatePage(NavAction.First);
        _menuNavLastPage.Click    += (_, _) => NavigatePage(NavAction.Last);
        _menuNavGoRightRoom.Click += (_, _) => NavigateAdjacentRoom(Direction.Right);
        _menuNavGoLeftRoom.Click  += (_, _) => NavigateAdjacentRoom(Direction.Left);
        _menuNavGoUpRoom.Click    += (_, _) => NavigateAdjacentRoom(Direction.Up);
        _menuNavGoDownRoom.Click  += (_, _) => NavigateAdjacentRoom(Direction.Down);
        _menuNavJumpPage.Click    += (_, _) => OpenJumpPageDialog();
        _menuNavBack.Click        += (_, _) => NavigateBack();
        _menuNavForward.Click     += (_, _) => NavigateForward();

        // ========================
        // Tools
        // ========================
        _menuToolsPen.Click       += (_, _) => SetToolMode(EditorToolMode.Pen);
        _menuToolsSelection.Click += (_, _) => SetToolMode(EditorToolMode.Selection);
        _menuToolsBucket.Click    += (_, _) => SetToolMode(EditorToolMode.Bucket);
        _menuToolsMarker.Click    += (_, _) => SetToolMode(EditorToolMode.Marker);
        _menuToolsObject.Click    += (_, _) => SetToolMode(EditorToolMode.Object);
        _menuToolsOptions.Click   += (_, _) => OpenEditorPropertiesDialog();

        // ========================
        // Window
        // ========================
        _menuWindowMapView.Click        += (_, _) => ShowMapViewContent();
        _menuWindowStageExplorer.Click  += (_, _) => ShowDockContent(_stageExplorerContent, DockState.DockLeft);
        _menuWindowProperties.Click     += (_, _) => ShowDockContent(_propertyWindowContent, DockState.DockRight);
        _menuWindowBookmarkList.Click   += (_, _) => ShowDockContent(_bookmarkListContent, DockState.DockLeft);
        _menuWindowObjectPalette.Click  += (_, _) => ShowDockContent(_objectPaletteContent, DockState.DockLeft);
        _menuWindowObjectList.Click     += (_, _) => ShowDockContent(_objectListContent, DockState.DockLeft);
        _menuWindowTagManager.Click     += (_, _) => OpenTagManager();
        _menuWindowMarkerManager.Click  += (_, _) => ShowDockContent(_markerColorPanelContent, DockState.DockRight);
        _menuWindowStageMapViewer.Click += (_, _) => OpenStageMapViewer();
        _menuWindowPageNodeEditor.Click += (_, _) => OpenNodeEditor();
        _menuWindowMetaTileEditor.Click += (_, _) => OpenMetaTileEditor();
        _menuWindowResetLayout.Click    += (_, _) => ResetDockLayout();

        // ========================
        // Help
        // ========================
        _menuHelpContents.Click += (_, _) => OpenHelpContents();

        _menuHelpShortcuts.Click += (_, _) =>
        {
            using var dialog = new ShortcutsDialog(_menuStrip);
            dialog.ShowDialog(this);
        };

        _menuHelpAbout.Click += (_, _) =>
        {
            using var dialog = new AboutBox();
            dialog.ShowDialog(this);
        };

        // ========================
        // Edit / View / Navigation メニューの有効状態管理
        // ========================
        _menuFile.DropDownOpening       += (_, _) => RefreshFileMenuState();
        _menuEdit.DropDownOpening       += (_, _) => RefreshEditMenuState();
        _menuView.DropDownOpening       += (_, _) => RefreshViewMenuState();
        _menuNavigation.DropDownOpening += (_, _) => RefreshNavigationMenuState();
    }

    // ========================
    // Edit メニュー有効状態
    // ========================
    private void RefreshFileMenuState()
    {
        var hasProject = _context.HasProject;
        var hasStage   = _context.HasStage;
        var isReadOnly = _commandManager.IsReadOnly;

        _menuFileSaveProject.Enabled   = hasProject && !isReadOnly;
        _menuFileSaveProjectAs.Enabled = hasProject && !isReadOnly;
        _menuFileCloseProject.Enabled  = hasProject;
        _menuFileNewStage.Enabled      = hasProject && !isReadOnly;
        _menuFileImportStage.Enabled   = hasProject && !isReadOnly;
        _menuFileSaveStage.Enabled     = hasStage && !isReadOnly;
        _menuFileReloadStage.Enabled   = hasStage;
        _menuFileDropStage.Enabled     = hasStage && !isReadOnly;
        _menuFileAddPage.Enabled       = hasStage && !isReadOnly;
        _menuFileDuplicatePage.Enabled = _context.HasPage && !isReadOnly;
        _menuFileRemovePage.Enabled    = _context.HasPage && !isReadOnly;
        _menuFileImportTileSet.Enabled = hasStage && !isReadOnly;
        _menuFileExportBin.Enabled     = hasStage;
        _menuFileExportAll.Enabled     = hasProject;
    }

     private void RefreshEditMenuState()
     {
        var isReadOnly = _commandManager.IsReadOnly;

        _menuEditUndo.Enabled   = _commandManager.CanUndo && !isReadOnly;
        _menuEditRedo.Enabled   = _commandManager.CanRedo && !isReadOnly;
        _menuEditCut.Enabled    = _context.HasPage && !isReadOnly;
        _menuEditCopy.Enabled   = _context.HasPage;
        _menuEditPaste.Enabled  = _context.HasPage && !isReadOnly;
        _menuEditDelete.Enabled = _context.HasPage && !isReadOnly;
        _menuEditFill.Enabled   = _context.HasPage && !isReadOnly;
        _menuEditToggleBookmark.Enabled = _context.HasPage && !isReadOnly;
        _menuEditClearAllBookmarks.Enabled = _context.Project?.Bookmarks.Count > 0 && !isReadOnly;
        _menuEditClearPageMarkers.Enabled = _context.HasPage && !isReadOnly && _markerState.HasMarkers;
        _menuEditClearStageMarkers.Enabled = _context.HasStage && !isReadOnly && _markerState.HasMarkers;
        _menuEditClearAllMarkers.Enabled = !isReadOnly && _markerState.HasMarkers;

        // NOTE: ブックマークの有無に応じてメニューのテキストを切り替える
        if (_context.CurrentStage is { } stage && _context.CurrentPage is { } page && _context.Project != null)
        {
            var isBookmarked = _context.Project.Bookmarks
                .Any(b => b.StageId == stage.Id && b.PageId == page.Id);
            _menuEditToggleBookmark.Text = isBookmarked ? "Remove Bookmark" : "Add Bookmark";
        }
    }

    // ========================
    // View メニュー有効状態
    // ========================
    private void RefreshViewMenuState()
    {
        var hasMapView = _mapViewContent is { IsDisposed: false };

        _menuViewZoomIn.Enabled = hasMapView && _mapView.CanZoomIn;
        _menuViewZoomOut.Enabled = hasMapView && _mapView.CanZoomOut;
        _menuViewResetZoom.Enabled = hasMapView && !_mapView.IsDefaultZoom;
    }

    // ========================
    // Navigation メニュー有効状態
    // ========================
    private void RefreshNavigationMenuState()
    {
        var page = _context.CurrentPage;

        _menuNavGoRightRoom.Enabled = page?.Header.RightPage != 0xFF;
        _menuNavGoLeftRoom.Enabled  = page?.Header.LeftPage  != 0xFF;
        _menuNavGoUpRoom.Enabled    = page?.Header.UpPage    != 0xFF;
        _menuNavGoDownRoom.Enabled  = page?.Header.DownPage  != 0xFF;

        _menuNavBack.Enabled = CanMoveZLayer(forward: false);
        _menuNavForward.Enabled = CanMoveZLayer(forward: true);
    }

    private void ImportTileSet() => OpenTilesetImage();
    private void ExportBin() => ExportCurrentStageBin();
    private void FillSelection() => ApplySelectionFill();
    private void ZoomIn() => _mapView.ZoomIn();
    private void ZoomOut() => _mapView.ZoomOut();
    private void ResetZoom() => _mapView.ResetZoom();

    // ========================
    // ヘルプメニュー
    // ========================
    private const string HelpContentsUrl = "https://www.loopunlimited-rootone.com/40000/sse.help/ja/index.html";

    private static void OpenHelpContents()
    {
        try
        {
            Process.Start(new ProcessStartInfo(HelpContentsUrl) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"ヘルプページを開けませんでした。\n{ex.Message}",
                "エラー",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning
            );
        }
    }
}
