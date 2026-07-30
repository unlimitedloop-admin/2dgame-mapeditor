using StageSmith.Core.Constants;
using StageSmith.Application.Services;
using StageSmith.Core.Models;
using StageSmith.Infrastructure.Persistence;

namespace StageSmith.Editor;

public partial class MainForm
{
    private readonly IConfigRepository _configRepository = new IniConfigRepository();
    private EditorConfig _config = new();

    /// <summary>
    /// 起動時にiniを読み込み、ウィンドウ位置・サイズへ反映する。
    /// 未保存（初回起動）の場合はCenterScreenへフォールバックする。
    /// </summary>
    private void LoadConfig()
    {
        _config = _configRepository.Load();
        ApplyWindowBoundsFromConfig();
    }

    private void ApplyWindowBoundsFromConfig()
    {
        if (_config.MainWindowWidth <= 0 || _config.MainWindowHeight <= 0)
        {
            StartPosition = FormStartPosition.CenterScreen;
            return;
        }

        StartPosition = FormStartPosition.Manual;
        Location = new Point(_config.MainWindowX, _config.MainWindowY);
        Size = new Size(_config.MainWindowWidth, _config.MainWindowHeight);

        if (_config.MainWindowMaximized)
            WindowState = FormWindowState.Maximized;
    }

    /// <summary>
    /// メインウィンドウの位置・サイズ・最大化状態を初期値へ戻す。
    /// Reset Window Layout から呼び出される想定。
    /// </summary>
    private void ResetMainWindowBounds()
    {
        if (WindowState != FormWindowState.Normal)
            WindowState = FormWindowState.Normal;

        ClientSize = ViewerConstants.MainFormClientSize;

        var workingArea = Screen.FromControl(this).WorkingArea;
        Location = new Point(
            workingArea.X + (workingArea.Width - Width) / 2,
            workingArea.Y + (workingArea.Height - Height) / 2);
    }

    /// <summary>
    /// 終了時に現在の状態をiniへ書き戻す。
    /// ③の拡張プロパティ項目は編集UI未実装のため、読み込んだ値をそのまま保持する。
    /// </summary>
    private void SaveConfig()
    {
        _config.LastOpenedProjectPath = _currentProjectPath;
        _config.LastSelectedStageIndex = _context.CurrentStageIndex;
        _config.LastSelectedPageIndex = _context.CurrentPageIndex;
        _config.LastToolMode = _currentMode;

        _config.ShowGridLines = _menuViewGridLines.Checked;
        _config.ShowTilePreview = _menuViewTilePreview.Checked;
        _config.ShowTileNumbers = _menuViewShowTileNumbers.Checked;
        _config.ShowRowNumbers = _menuViewRowNumbers.Checked;
        _config.ShowColumnNumbers = _menuViewColumnNumbers.Checked;
        _config.ShowTileInfo = _menuViewTileInfo.Checked;
        _config.ShowMarkerOverlay = _menuViewMarkerOverlay.Checked;
        _config.ShowToolBar = _menuViewToolBar.Checked;
        _config.ShowStatusBar = _menuViewStatusBar.Checked;

        var bounds = WindowState == FormWindowState.Normal ? Bounds : RestoreBounds;
        _config.MainWindowX = bounds.X;
        _config.MainWindowY = bounds.Y;
        _config.MainWindowWidth = bounds.Width;
        _config.MainWindowHeight = bounds.Height;
        _config.MainWindowMaximized = WindowState == FormWindowState.Maximized;

        _configRepository.Save(_config);
    }

    /// <summary>
    /// Viewメニュー状態・ツール状態をコンフィグから復元する。
    /// 既存の CheckedChanged ハンドラ（MainForm.MenuHandlers.cs）を極力再利用し、
    /// 「_menuViewXxx.Checked が単一の真実」という既存方針を崩さない。
    /// </summary>
    private void ApplyViewStateFromConfig()
    {
        _menuViewGridLines.Checked = _config.ShowGridLines;
        ApplyGridState(_config.ShowGridLines);

        _menuViewTilePreview.Checked = _config.ShowTilePreview;
        ApplyTilePreviewState(_config.ShowTilePreview);
        UpdateTilePreviewIcon();

        _menuViewShowTileNumbers.Checked = _config.ShowTileNumbers;
        ToggleShowTileNumbers(_config.ShowTileNumbers);

        _menuViewRowNumbers.Checked = _config.ShowRowNumbers;
        ApplyRowNumberState(_config.ShowRowNumbers);

        _menuViewColumnNumbers.Checked = _config.ShowColumnNumbers;
        ApplyColumnNumberState(_config.ShowColumnNumbers);

        _menuViewTileInfo.Checked = _config.ShowTileInfo;
        _mapView.SetShowTileInfo(_config.ShowTileInfo);

        _menuViewMarkerOverlay.Checked = _config.ShowMarkerOverlay;
        ToggleMarkerOverlay(_config.ShowMarkerOverlay);

        _menuViewToolBar.Checked = _config.ShowToolBar;
        _editorToolStrip.Visible = _config.ShowToolBar;

        _menuViewStatusBar.Checked = _config.ShowStatusBar;
        _statusStrip.Visible = _config.ShowStatusBar;
    }

    /// <summary>
    /// 直前に使用していたツール（Pen/Selection/Marker）を復元する。
    /// ツール群の初期化（InitializeTools）完了後に呼び出すこと。
    /// </summary>
    private void ApplyToolModeFromConfig()
    {
        SetToolMode(_config.LastToolMode);
        UpdateToolbarCheckedState();
    }
}
