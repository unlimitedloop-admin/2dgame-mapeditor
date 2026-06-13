using StageSmith.Core.Constants;
using StageSmith.Editor.Controls;

namespace StageSmith.Editor
{
    partial class MainForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();

            // ========================
            // MenuStrip
            // ========================
            _menuStrip = new MenuStrip();

            // ========================
            // File
            // ========================
            _menuFile               = new ToolStripMenuItem("File");
            _menuFileNewProject     = new ToolStripMenuItem("New Project")      { ShortcutKeys = Keys.Control | Keys.P };
            _menuFileOpenProject    = new ToolStripMenuItem("Open Project")     { ShortcutKeys = Keys.Control | Keys.Shift | Keys.O };
            _menuFileSaveProject    = new ToolStripMenuItem("Save Project")     { ShortcutKeys = Keys.Control | Keys.Shift | Keys.S };
            _menuFileCloseProject   = new ToolStripMenuItem("Close Project");
            _menuFileNewStage       = new ToolStripMenuItem("New Stage")        { ShortcutKeys = Keys.Control | Keys.N };
            _menuFileSaveStage      = new ToolStripMenuItem("Save Stage")       { ShortcutKeys = Keys.Control | Keys.S };
            _menuFileReloadStage    = new ToolStripMenuItem("Reload Stage");
            _menuFileDropStage      = new ToolStripMenuItem("Drop Stage");
            _menuFileImportTileSet  = new ToolStripMenuItem("Import TileSet");
            _menuFileExportBin      = new ToolStripMenuItem("Export BIN");
            _menuFileExportAll      = new ToolStripMenuItem("Export All Stages");
            _menuFileQuit           = new ToolStripMenuItem("Quit")             { ShortcutKeys = Keys.Alt | Keys.F4 };

            _menuFile.DropDownItems.AddRange([
                _menuFileNewProject,
                _menuFileOpenProject,
                _menuFileSaveProject,
                _menuFileCloseProject,
                new ToolStripSeparator(),
                _menuFileNewStage,
                _menuFileSaveStage,
                _menuFileReloadStage,
                _menuFileDropStage,
                new ToolStripSeparator(),
                _menuFileImportTileSet,
                _menuFileExportBin,
                _menuFileExportAll,
                new ToolStripSeparator(),
                _menuFileQuit,
            ]);

            // ========================
            // Edit
            // ========================
            _menuEdit                   = new ToolStripMenuItem("Edit");
            _menuEditUndo               = new ToolStripMenuItem("Undo")                     { ShortcutKeys = Keys.Control | Keys.Z };
            _menuEditRedo               = new ToolStripMenuItem("Redo")                     { ShortcutKeys = Keys.Control | Keys.Y };
            _menuEditCut                = new ToolStripMenuItem("Cut")                      { ShortcutKeys = Keys.Control | Keys.X };
            _menuEditCopy               = new ToolStripMenuItem("Copy")                     { ShortcutKeys = Keys.Control | Keys.C };
            _menuEditPaste              = new ToolStripMenuItem("Paste")                    { ShortcutKeys = Keys.Control | Keys.V };
            _menuEditDelete             = new ToolStripMenuItem("Delete")                   { ShortcutKeys = Keys.Delete };
            _menuEditFill               = new ToolStripMenuItem("Fill")                     { ShortcutKeyDisplayString = "F" };
            _menuEditSelectAllSameTile  = new ToolStripMenuItem("Select All Same Tile")     { ShortcutKeys = Keys.Control | Keys.Shift | Keys.A };
            _menuEditSelectEmptyTile    = new ToolStripMenuItem("Select Empty Tile")        { ShortcutKeys = Keys.Control | Keys.Shift | Keys.E };
            _menuEditInvertSelection    = new ToolStripMenuItem("Invert Selection")         { ShortcutKeys = Keys.Control | Keys.I };
            _menuEditClearSelection     = new ToolStripMenuItem("Clear Selection")          { ShortcutKeyDisplayString = "Esc" };
            _menuEditFindTile           = new ToolStripMenuItem("Find Tile...")             { ShortcutKeys = Keys.Control | Keys.F };
            _menuEditFindTileOnMap      = new ToolStripMenuItem("Find Tile On Map")         { ShortcutKeys = Keys.F3 };
            _menuEditFindNextTile       = new ToolStripMenuItem("Find Next Tile")           { ShortcutKeys = Keys.F4 };
            _menuEditFindPrevTile       = new ToolStripMenuItem("Find Previous Tile")       { ShortcutKeys = Keys.Shift | Keys.F4 };
            _menuEditReplaceTile        = new ToolStripMenuItem("Replace Tile...")          { ShortcutKeys = Keys.Control | Keys.H };
            _menuEditClearSearchHighlight = new ToolStripMenuItem("Clear Search Highlight") { ShortcutKeyDisplayString = "Shift+Esc" };

            _menuEdit.DropDownItems.AddRange([
                _menuEditUndo,
                _menuEditRedo,
                new ToolStripSeparator(),
                _menuEditCut,
                _menuEditCopy,
                _menuEditPaste,
                _menuEditDelete,
                new ToolStripSeparator(),
                _menuEditFill,
                new ToolStripSeparator(),
                _menuEditSelectAllSameTile,
                _menuEditSelectEmptyTile,
                _menuEditInvertSelection,
                _menuEditClearSelection,
                new ToolStripSeparator(),
                _menuEditFindTile,
                _menuEditFindTileOnMap,
                _menuEditFindNextTile,
                _menuEditFindPrevTile,
                _menuEditReplaceTile,
                _menuEditClearSearchHighlight,
            ]);

            // ========================
            // View
            // ========================
            _menuView                   = new ToolStripMenuItem("View");
            _menuViewGridLines          = new ToolStripMenuItem("Grid Lines")           { ShortcutKeyDisplayString = "G", CheckOnClick = true };
            _menuViewShowTileNumbers    = new ToolStripMenuItem("Show Tile Numbers")    { ShortcutKeyDisplayString = "L", CheckOnClick = true };
            _menuViewRowNumbers         = new ToolStripMenuItem("Row Numbers")          { ShortcutKeyDisplayString = "R", CheckOnClick = true };
            _menuViewColumnNumbers      = new ToolStripMenuItem("Column Numbers")       { ShortcutKeyDisplayString = "C", CheckOnClick = true };
            _menuViewTileInfo           = new ToolStripMenuItem("Tile Info")            { ShortcutKeyDisplayString = "I", CheckOnClick = true };
            _menuViewMarkerOverlay      = new ToolStripMenuItem("Marker Overlay")       { ShortcutKeyDisplayString = "M", CheckOnClick = true };
            _menuViewZoomIn             = new ToolStripMenuItem("Zoom In")              { ShortcutKeys = Keys.Control | Keys.Oemplus };
            _menuViewZoomOut            = new ToolStripMenuItem("Zoom Out")             { ShortcutKeys = Keys.Control | Keys.OemMinus };
            _menuViewResetZoom          = new ToolStripMenuItem("Reset Zoom")           { ShortcutKeys = Keys.Control | Keys.D0 };
            _menuViewRuler              = new ToolStripMenuItem("Ruler")                { ShortcutKeyDisplayString = "U", CheckOnClick = true };
            _menuViewPageBoundary       = new ToolStripMenuItem("Page Boundary")        { ShortcutKeyDisplayString = "B", CheckOnClick = true };
            _menuViewToolBar            = new ToolStripMenuItem("Tool Bar")             { ShortcutKeyDisplayString = "T", CheckOnClick = true, Checked = true };
            _menuViewStatusBar          = new ToolStripMenuItem("Status Bar")           { ShortcutKeyDisplayString = "S", CheckOnClick = true, Checked = true };

            _menuView.DropDownItems.AddRange([
                _menuViewGridLines,
                _menuViewShowTileNumbers,
                _menuViewRowNumbers,
                _menuViewColumnNumbers,
                _menuViewTileInfo,
                _menuViewMarkerOverlay,
                new ToolStripSeparator(),
                _menuViewZoomIn,
                _menuViewZoomOut,
                _menuViewResetZoom,
                new ToolStripSeparator(),
                _menuViewRuler,
                _menuViewPageBoundary,
                new ToolStripSeparator(),
                _menuViewToolBar,
                _menuViewStatusBar,
            ]);

            // ========================
            // Navigation
            // ========================
            _menuNavigation     = new ToolStripMenuItem("Navigation");
            _menuNavNextPage    = new ToolStripMenuItem("Next Page")        { ShortcutKeyDisplayString = "PageDown" };
            _menuNavPrevPage    = new ToolStripMenuItem("Previous Page")    { ShortcutKeyDisplayString = "PageUp" };
            _menuNavFirstPage   = new ToolStripMenuItem("First Page")       { ShortcutKeys = Keys.Control | Keys.Home };
            _menuNavLastPage    = new ToolStripMenuItem("Last Page")        { ShortcutKeys = Keys.Control | Keys.End };
            _menuNavGoRightRoom = new ToolStripMenuItem("Go Right Room")    { ShortcutKeys = Keys.Control | Keys.Right };
            _menuNavGoLeftRoom  = new ToolStripMenuItem("Go Left Room")     { ShortcutKeys = Keys.Control | Keys.Left };
            _menuNavGoUpRoom    = new ToolStripMenuItem("Go Up Room")       { ShortcutKeys = Keys.Control | Keys.Up };
            _menuNavGoDownRoom  = new ToolStripMenuItem("Go Down Room")     { ShortcutKeys = Keys.Control | Keys.Down };
            _menuNavJumpPage    = new ToolStripMenuItem("Jump Page...")     { ShortcutKeys = Keys.Control | Keys.J };
            _menuNavBack        = new ToolStripMenuItem("Back")             { ShortcutKeys = Keys.Alt | Keys.Left };
            _menuNavForward     = new ToolStripMenuItem("Forward")          { ShortcutKeys = Keys.Alt | Keys.Right };

            _menuNavigation.DropDownItems.AddRange([
                _menuNavNextPage,
                _menuNavPrevPage,
                _menuNavFirstPage,
                _menuNavLastPage,
                new ToolStripSeparator(),
                _menuNavGoRightRoom,
                _menuNavGoLeftRoom,
                _menuNavGoUpRoom,
                _menuNavGoDownRoom,
                new ToolStripSeparator(),
                _menuNavJumpPage,
                new ToolStripSeparator(),
                _menuNavBack,
                _menuNavForward,
            ]);

            // ========================
            // Window
            // ========================
            _menuWindow                 = new ToolStripMenuItem("Window");
            _menuWindowStageExplorer    = new ToolStripMenuItem("Stage Explorer")       { ShortcutKeys = Keys.F7 };
            _menuWindowProperties       = new ToolStripMenuItem("Properties Window")    { ShortcutKeys = Keys.F8 };
            _menuWindowBookmarkList     = new ToolStripMenuItem("Bookmark List")        { ShortcutKeys = Keys.F9 };
            _menuWindowTagManager       = new ToolStripMenuItem("Tag Manager")          { ShortcutKeys = Keys.Control | Keys.F7 };
            _menuWindowMarkerManager    = new ToolStripMenuItem("Marker Manager")       { ShortcutKeys = Keys.Control | Keys.F8 };
            _menuWindowStageMapViewer   = new ToolStripMenuItem("Stage Map Viewer")     { ShortcutKeys = Keys.F10 };
            _menuWindowPageNodeEditor   = new ToolStripMenuItem("Page Node Editor")     { ShortcutKeys = Keys.F11 };
            _menuWindowMetaTileEditor   = new ToolStripMenuItem("MetaTile Editor")      { ShortcutKeys = Keys.F12 };
            _menuWindowResetLayout      = new ToolStripMenuItem("Reset Window Layout");

            _menuWindow.DropDownItems.AddRange([
                _menuWindowStageExplorer,
                _menuWindowProperties,
                _menuWindowBookmarkList,
                new ToolStripSeparator(),
                _menuWindowTagManager,
                _menuWindowMarkerManager,
                new ToolStripSeparator(),
                _menuWindowStageMapViewer,
                _menuWindowPageNodeEditor,
                _menuWindowMetaTileEditor,
                new ToolStripSeparator(),
                _menuWindowResetLayout,
            ]);

            // ========================
            // Help
            // ========================
            _menuHelp           = new ToolStripMenuItem("Help");
            _menuHelpContents   = new ToolStripMenuItem("Contents") { ShortcutKeys = Keys.F1 };
            _menuHelpShortcuts  = new ToolStripMenuItem("Shortcuts");
            _menuHelpAbout      = new ToolStripMenuItem("About StageSmith Editor");

            _menuHelp.DropDownItems.AddRange([
                _menuHelpContents,
                _menuHelpShortcuts,
                _menuHelpAbout,
            ]);

            // ========================
            // MenuStrip に追加
            // ========================
            _menuStrip.Items.AddRange([
                _menuFile,
                _menuEdit,
                _menuView,
                _menuNavigation,
                _menuWindow,
                _menuHelp,
            ]);

            // ========================
            // Form 設定
            // ========================
            SuspendLayout();
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode       = AutoScaleMode.Font;
            ClientSize          = new Size(1200, 900);
            Name                = "MainForm";
            Text                = "StageSmith Editor";
            MainMenuStrip       = _menuStrip;
            Load               += MainForm_Load;

            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
        // ========================
        // MenuStrip
        // ========================
        private MenuStrip _menuStrip = null!;

        // File
        private ToolStripMenuItem _menuFile               = null!;
        private ToolStripMenuItem _menuFileNewProject     = null!;
        private ToolStripMenuItem _menuFileOpenProject    = null!;
        private ToolStripMenuItem _menuFileSaveProject    = null!;
        private ToolStripMenuItem _menuFileCloseProject   = null!;
        private ToolStripMenuItem _menuFileNewStage       = null!;
        private ToolStripMenuItem _menuFileSaveStage      = null!;
        private ToolStripMenuItem _menuFileReloadStage    = null!;
        private ToolStripMenuItem _menuFileDropStage      = null!;
        private ToolStripMenuItem _menuFileImportTileSet  = null!;
        private ToolStripMenuItem _menuFileExportBin      = null!;
        private ToolStripMenuItem _menuFileExportAll      = null!;
        private ToolStripMenuItem _menuFileQuit           = null!;

        // Edit
        private ToolStripMenuItem _menuEdit                     = null!;
        private ToolStripMenuItem _menuEditUndo                 = null!;
        private ToolStripMenuItem _menuEditRedo                 = null!;
        private ToolStripMenuItem _menuEditCut                  = null!;
        private ToolStripMenuItem _menuEditCopy                 = null!;
        private ToolStripMenuItem _menuEditPaste                = null!;
        private ToolStripMenuItem _menuEditDelete               = null!;
        private ToolStripMenuItem _menuEditFill                 = null!;
        private ToolStripMenuItem _menuEditSelectAllSameTile    = null!;
        private ToolStripMenuItem _menuEditSelectEmptyTile      = null!;
        private ToolStripMenuItem _menuEditInvertSelection      = null!;
        private ToolStripMenuItem _menuEditClearSelection       = null!;
        private ToolStripMenuItem _menuEditFindTile             = null!;
        private ToolStripMenuItem _menuEditFindTileOnMap        = null!;
        private ToolStripMenuItem _menuEditFindNextTile         = null!;
        private ToolStripMenuItem _menuEditFindPrevTile         = null!;
        private ToolStripMenuItem _menuEditReplaceTile          = null!;
        private ToolStripMenuItem _menuEditClearSearchHighlight = null!;

        // View
        private ToolStripMenuItem _menuView                = null!;
        private ToolStripMenuItem _menuViewGridLines       = null!;
        private ToolStripMenuItem _menuViewShowTileNumbers = null!;
        private ToolStripMenuItem _menuViewRowNumbers      = null!;
        private ToolStripMenuItem _menuViewColumnNumbers   = null!;
        private ToolStripMenuItem _menuViewTileInfo        = null!;
        private ToolStripMenuItem _menuViewMarkerOverlay   = null!;
        private ToolStripMenuItem _menuViewZoomIn          = null!;
        private ToolStripMenuItem _menuViewZoomOut         = null!;
        private ToolStripMenuItem _menuViewResetZoom       = null!;
        private ToolStripMenuItem _menuViewRuler           = null!;
        private ToolStripMenuItem _menuViewPageBoundary    = null!;
        private ToolStripMenuItem _menuViewToolBar         = null!;
        private ToolStripMenuItem _menuViewStatusBar       = null!;

        // Navigation
        private ToolStripMenuItem _menuNavigation     = null!;
        private ToolStripMenuItem _menuNavNextPage    = null!;
        private ToolStripMenuItem _menuNavPrevPage    = null!;
        private ToolStripMenuItem _menuNavFirstPage   = null!;
        private ToolStripMenuItem _menuNavLastPage    = null!;
        private ToolStripMenuItem _menuNavGoRightRoom = null!;
        private ToolStripMenuItem _menuNavGoLeftRoom  = null!;
        private ToolStripMenuItem _menuNavGoUpRoom    = null!;
        private ToolStripMenuItem _menuNavGoDownRoom  = null!;
        private ToolStripMenuItem _menuNavJumpPage    = null!;
        private ToolStripMenuItem _menuNavBack        = null!;
        private ToolStripMenuItem _menuNavForward     = null!;

        // Window
        private ToolStripMenuItem _menuWindow               = null!;
        private ToolStripMenuItem _menuWindowStageExplorer  = null!;
        private ToolStripMenuItem _menuWindowProperties     = null!;
        private ToolStripMenuItem _menuWindowBookmarkList   = null!;
        private ToolStripMenuItem _menuWindowTagManager     = null!;
        private ToolStripMenuItem _menuWindowMarkerManager  = null!;
        private ToolStripMenuItem _menuWindowStageMapViewer = null!;
        private ToolStripMenuItem _menuWindowPageNodeEditor = null!;
        private ToolStripMenuItem _menuWindowMetaTileEditor = null!;
        private ToolStripMenuItem _menuWindowResetLayout    = null!;

        // Help
        private ToolStripMenuItem _menuHelp          = null!;
        private ToolStripMenuItem _menuHelpContents  = null!;
        private ToolStripMenuItem _menuHelpShortcuts = null!;
        private ToolStripMenuItem _menuHelpAbout     = null!;
    }
}
