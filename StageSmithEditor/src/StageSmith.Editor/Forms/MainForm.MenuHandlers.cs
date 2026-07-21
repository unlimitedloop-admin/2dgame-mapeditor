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
        _menuFileCloseProject.Click  += (_, _) => CloseProject();
        _menuFileNewStage.Click      += (_, _) => NewStage();
        _menuFileImportStage.Click   += (_, _) => ImportStage();
        _menuFileSaveStage.Click     += (_, _) => SaveStage();
        _menuFileReloadStage.Click   += (_, _) => ReloadStage();
        _menuFileDropStage.Click     += (_, _) => DropStage();
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

        // ========================
        // View
        // ========================
        _menuViewGridLines.CheckedChanged       += (_, _) => ApplyGridState(_menuViewGridLines.Checked);
        _menuViewTilePreview.CheckedChanged     += (_, _) => ApplyTilePreviewState(_menuViewTilePreview.Checked);
        _menuViewShowTileNumbers.CheckedChanged += (_, _) => ToggleShowTileNumbers(_menuViewShowTileNumbers.Checked);
        _menuViewRowNumbers.CheckedChanged      += (_, _) => ApplyRowNumberState(_menuViewRowNumbers.Checked);
        _menuViewColumnNumbers.CheckedChanged   += (_, _) => ApplyColumnNumberState(_menuViewColumnNumbers.Checked);
        _menuViewMarkerOverlay.CheckedChanged   += (_, _) => ToggleMarkerOverlay(_menuViewMarkerOverlay.Checked);
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
        // Window
        // ========================
        _menuWindowMapView.Click        += (_, _) => ShowMapViewContent();
        _menuWindowStageExplorer.Click  += (_, _) => ShowDockContent(_stageExplorerContent, DockState.DockLeft);
        _menuWindowProperties.Click     += (_, _) => ShowDockContent(_propertyWindowContent, DockState.DockRight);
        _menuWindowBookmarkList.Click   += (_, _) => ShowDockContent(_bookmarkListContent, DockState.DockLeft);
        _menuWindowTagManager.Click     += (_, _) => { /* TODO: TagManager */ };
        _menuWindowMarkerManager.Click  += (_, _) => { /* TODO: MarkerManager */ };
        _menuWindowStageMapViewer.Click += (_, _) => OpenStageMapViewer();
        _menuWindowPageNodeEditor.Click += (_, _) => OpenNodeEditor();
        _menuWindowMetaTileEditor.Click += (_, _) => OpenMetaTileEditor();
        _menuWindowResetLayout.Click    += (_, _) => ResetDockLayout();

        // ========================
        // Help
        // ========================
        _menuHelpAbout.Click += (_, _) =>
            MessageBox.Show(
                "StageSmith Editor\nVersion 0.9",
                "About",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information
            );

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

        _menuFileSaveProject.Enabled   = hasProject;
        _menuFileCloseProject.Enabled  = hasProject;
        _menuFileNewStage.Enabled      = hasProject;
        _menuFileImportStage.Enabled   = hasProject;
        _menuFileSaveStage.Enabled     = hasStage;
        _menuFileReloadStage.Enabled   = hasStage;
        _menuFileDropStage.Enabled     = hasStage;
        _menuFileImportTileSet.Enabled = hasStage;
        _menuFileExportBin.Enabled     = hasStage;
        _menuFileExportAll.Enabled     = hasProject;
    }

    private void RefreshEditMenuState()
    {
        _menuEditUndo.Enabled   = _commandManager.CanUndo;
        _menuEditRedo.Enabled   = _commandManager.CanRedo;
        _menuEditCut.Enabled    = _context.HasPage;
        _menuEditCopy.Enabled   = _context.HasPage;
        _menuEditPaste.Enabled  = _context.HasPage;
        _menuEditDelete.Enabled = _context.HasPage;
        _menuEditFill.Enabled   = _context.HasPage;
        _menuEditToggleBookmark.Enabled = _context.HasPage;

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

    // ========================
    // 未実装スタブ（後続タスクで実装）
    // ========================
    //private void OpenProject() { }        // MainForm.FileOperations.cs で定義済み
    //private void SaveProject() { }        // MainForm.FileOperations.cs で定義済み
    //private void CloseProject() { }       // MainForm.FileOperations.cs で定義済み
    //private void NewStage() { }           // MainForm.FileOperations.cs で定義済み
    private void SaveStage() => ExportCurrentStageDefAs();
    private void ReloadStage()          { /* TODO */ }
    private void DropStage()            { /* TODO */ }
    private void ImportTileSet() => OpenTilesetImage();
    private void ExportBin() => ExportCurrentStageBin();
    //private void ExportAllStages() { }    // MainForm.FileOperations.cs で定義済み
    private void CutSelection()         { /* TODO */ }
    //private void CopySelection() { }      // MainForm.Commands.cs で定義済み
    private void PasteSelection()       { /* TODO */ }
    //private void DeleteSelection() { }    // MainForm.Commands.cs で定義済み
    private void FillSelection()        { /* TODO */ }
    private void SelectAllSameTile()    { /* TODO */ }
    private void SelectEmptyTile()      { /* TODO */ }
    private void InvertSelection()      { /* TODO */ }
    //private void ClearSelection() { }     // MainForm.Commands.cs で定義済み
    // OpenFindTileDialog / FindTileOnMap / FindNextTile / FindPrevTile / ClearSearchHighlight は MainForm.Search.cs で定義済み
    //private void OpenReplaceTileDialog(){ }   // MainForm.Search.cs で定義済み
    private void ToggleShowTileNumbers(bool show) { /* TODO */ }
    private void ToggleMarkerOverlay(bool show)   { /* TODO */ }
    private void ZoomIn() => _mapView.ZoomIn();
    private void ZoomOut() => _mapView.ZoomOut();
    private void ResetZoom() => _mapView.ResetZoom();
    private void OpenJumpPageDialog()   { /* TODO */ }
}
