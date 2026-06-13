using StageSmith.Editor.Controls;

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

        // ========================
        // View
        // ========================
        _menuViewGridLines.CheckedChanged       += (_, _) => ShowMapViewGrid();
        _menuViewShowTileNumbers.CheckedChanged += (_, _) => ToggleShowTileNumbers(_menuViewShowTileNumbers.Checked);
        _menuViewMarkerOverlay.CheckedChanged   += (_, _) => ToggleMarkerOverlay(_menuViewMarkerOverlay.Checked);
        _menuViewZoomIn.Click                   += (_, _) => ZoomIn();
        _menuViewZoomOut.Click                  += (_, _) => ZoomOut();
        _menuViewResetZoom.Click                += (_, _) => ResetZoom();

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
        _menuWindowStageExplorer.Click  += (_, _) => _stageExplorerContent.Show(_dockPanel);
        _menuWindowProperties.Click     += (_, _) => _propertyWindowContent.Show(_dockPanel);
        _menuWindowBookmarkList.Click   += (_, _) => { /* TODO: BookmarkList */ };
        _menuWindowTagManager.Click     += (_, _) => { /* TODO: TagManager */ };
        _menuWindowMarkerManager.Click  += (_, _) => { /* TODO: MarkerManager */ };
        _menuWindowStageMapViewer.Click += (_, _) => { /* TODO: StageMapViewer */ };
        _menuWindowPageNodeEditor.Click += (_, _) => OpenNodeEditor();
        _menuWindowMetaTileEditor.Click += (_, _) => { /* TODO: MetaTileEditor */ };
        _menuWindowResetLayout.Click    += (_, _) => InitializeDockLayout();

        // ========================
        // Help
        // ========================
        _menuHelpAbout.Click += (_, _) =>
            MessageBox.Show(
                "StageSmith Editor\nVersion 0.1",
                "About",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information
            );

        // ========================
        // Edit / Navigation メニューの有効状態管理
        // ========================
        _menuEdit.DropDownOpening       += (_, _) => RefreshEditMenuState();
        _menuNavigation.DropDownOpening += (_, _) => RefreshNavigationMenuState();
    }

    // ========================
    // Edit メニュー有効状態
    // ========================
    private void RefreshEditMenuState()
    {
        _menuEditUndo.Enabled   = _commandManager.CanUndo;
        _menuEditRedo.Enabled   = _commandManager.CanRedo;
        _menuEditCut.Enabled    = _context.HasPage;
        _menuEditCopy.Enabled   = _context.HasPage;
        _menuEditPaste.Enabled  = _context.HasPage;
        _menuEditDelete.Enabled = _context.HasPage;
        _menuEditFill.Enabled   = _context.HasPage;
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
    }

    // ========================
    // 隣接ページ移動
    // ========================
    private void NavigateAdjacentRoom(Direction direction)
    {
        var page = _context.CurrentPage;
        if (page == null) return;

        var targetRoomId = direction switch
        {
            Direction.Right => page.Header.RightPage,
            Direction.Left  => page.Header.LeftPage,
            Direction.Up    => page.Header.UpPage,
            Direction.Down  => page.Header.DownPage,
            _               => (byte)0xFF,
        };

        if (targetRoomId == 0xFF) return;

        var stage = _context.CurrentStage;
        if (stage == null) return;

        var targetIndex = stage.Pages
            .FindIndex(p => p.Header.RoomId == targetRoomId);

        if (targetIndex < 0) return;

        _context.SetPage(targetIndex);
        ApplyContextToView();
        _pageNavBar.UpdateDisplay(_context);
        _nodeEditorForm?.SyncPageSelection(_context.CurrentPageIndex);
    }

    // ========================
    // 未実装スタブ（後続タスクで実装）
    // ========================
    //private void OpenProject() { }        // MainForm.FileOperations.cs で定義済み
    //private void SaveProject() { }        // MainForm.FileOperations.cs で定義済み
    private void CloseProject()         { /* TODO */ }
    private void NewStage()             { /* TODO */ }
    private void SaveStage()            { /* TODO */ }
    private void ReloadStage()          { /* TODO */ }
    private void DropStage()            { /* TODO */ }
    private void ImportTileSet()        { /* TODO */ }
    private void ExportBin()            { /* TODO */ }
    private void ExportAllStages()      { /* TODO */ }
    private void CutSelection()         { /* TODO */ }
    //private void CopySelection() { }      // MainForm.Commands.cs で定義済み
    private void PasteSelection()       { /* TODO */ }
    //private void DeleteSelection() { }    // MainForm.Commands.cs で定義済み
    private void FillSelection()        { /* TODO */ }
    private void SelectAllSameTile()    { /* TODO */ }
    private void SelectEmptyTile()      { /* TODO */ }
    private void InvertSelection()      { /* TODO */ }
    //private void ClearSelection() { }     // MainForm.Commands.cs で定義済み
    private void OpenFindTileDialog()   { /* TODO */ }
    private void FindTileOnMap()        { /* TODO */ }
    private void FindNextTile()         { /* TODO */ }
    private void FindPrevTile()         { /* TODO */ }
    private void OpenReplaceTileDialog(){ /* TODO */ }
    private void ClearSearchHighlight() { /* TODO */ }
    private void ToggleShowTileNumbers(bool show) { /* TODO */ }
    private void ToggleMarkerOverlay(bool show)   { /* TODO */ }
    private void ZoomIn()               { /* TODO */ }
    private void ZoomOut()              { /* TODO */ }
    private void ResetZoom()            { /* TODO */ }
    private void OpenJumpPageDialog()   { /* TODO */ }
    private void NavigateBack()         { /* TODO */ }
    private void NavigateForward()      { /* TODO */ }
}
