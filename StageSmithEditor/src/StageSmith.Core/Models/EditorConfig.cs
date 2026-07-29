using StageSmith.Core.Constants;

namespace StageSmith.Core.Models;

/// <summary>
/// エディタの作業状態・UI状態・拡張プロパティ設定を保持するモデル。
/// プロジェクトデータ（.sseproj / .ssestage）とは独立して管理する。
/// </summary>
public class EditorConfig
{
    //========================
    // 基盤（④）
    //========================
    public string? LastOpenedProjectPath { get; set; }
    public int LastSelectedStageIndex { get; set; } = -1;
    public int LastSelectedPageIndex { get; set; } = -1;
    public int LastZ { get; set; } = 0;
    public EditorToolMode LastToolMode { get; set; } = EditorToolMode.Pen;

    // MainWindow の位置・サイズ（-1 は未保存を示すセンチネル値）
    public int MainWindowX { get; set; } = -1;
    public int MainWindowY { get; set; } = -1;
    public int MainWindowWidth { get; set; } = -1;
    public int MainWindowHeight { get; set; } = -1;
    public bool MainWindowMaximized { get; set; } = false;

    //========================
    // 拡張プロパティ（③）
    //========================
    public NumberDisplayFormat NumberDisplayFormat { get; set; } = NumberDisplayFormat.Hex;
    public bool KeepSelectedTileOnPageChange { get; set; } = false;
    public bool UseStageSubFolder { get; set; } = false;
    public bool UseProjectSubDirectory { get; set; } = false;
    public string? DefaultProjectSaveDirectory { get; set; }
    public byte DefaultClearTileId { get; set; } = 0;
}
