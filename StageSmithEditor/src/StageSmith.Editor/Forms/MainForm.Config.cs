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

        var bounds = WindowState == FormWindowState.Normal ? Bounds : RestoreBounds;
        _config.MainWindowX = bounds.X;
        _config.MainWindowY = bounds.Y;
        _config.MainWindowWidth = bounds.Width;
        _config.MainWindowHeight = bounds.Height;
        _config.MainWindowMaximized = WindowState == FormWindowState.Maximized;

        _configRepository.Save(_config);
    }
}
