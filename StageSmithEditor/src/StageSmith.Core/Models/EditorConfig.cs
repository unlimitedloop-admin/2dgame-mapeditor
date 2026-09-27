using StageSmith.Core.Constants;

namespace StageSmith.Core.Models;

/// <summary>
/// エディタの作業状態・UI状態・拡張プロパティ設定を保持するモデル。
/// プロジェクトデータ（.sseproj / .ssestage）とは独立して管理する。
/// </summary>
public class EditorConfig
{
    //========================
    // 基盤
    //========================
    public string? LastOpenedProjectPath { get; set; }
    public int LastSelectedStageIndex { get; set; } = -1;
    public int LastSelectedPageIndex { get; set; } = -1;
    public int LastZ { get; set; } = 0;

    /// <summary>オブジェクト配置のスナップ間隔（px）。1 ならスナップなし。</summary>
    public int EntitySnapSize { get; set; } = 1;
    public EditorToolMode LastToolMode { get; set; } = EditorToolMode.Pen;

    // MainWindow の位置・サイズ（-1 は未保存を示すセンチネル値）
    public int MainWindowX { get; set; } = -1;
    public int MainWindowY { get; set; } = -1;
    public int MainWindowWidth { get; set; } = -1;
    public int MainWindowHeight { get; set; } = -1;
    public bool MainWindowMaximized { get; set; } = false;

    // View メニュー状態（Designerの初期Checked値に合わせたデフォルト）
    public bool ShowGridLines { get; set; } = true;
    public bool ShowTilePreview { get; set; } = false;
    public bool ShowTileNumbers { get; set; } = false;
    public bool ShowRowNumbers { get; set; } = false;
    public bool ShowColumnNumbers { get; set; } = false;
    public bool ShowTileInfo { get; set; } = false;
    public bool ShowMarkerOverlay { get; set; } = false;
    public bool ShowEntities { get; set; } = true;
    public bool ShowToolBar { get; set; } = true;
    public bool ShowStatusBar { get; set; } = true;

    //========================
    // 拡張プロパティ
    //========================
    public NumberDisplayFormat NumberDisplayFormat { get; set; } = NumberDisplayFormat.Hex;
    public bool KeepSelectedTileOnPageChange { get; set; } = false;
    public bool ShowNodePreview { get; set; } = true;
    public bool UseStageSubFolder { get; set; } = false;
    public bool UseProjectSubDirectory { get; set; } = false;
    public string? DefaultProjectSaveDirectory { get; set; }
    public byte DefaultClearTileId { get; set; } = 0;

    /// <summary>
    /// trueの場合、Export BIN / Export All Stages のダイアログ初期表示フォルダを
    /// ステージファイルの保存先ディレクトリにする。
    /// </summary>
    public bool UseStageDirectoryForExport { get; set; } = false;

    /// <summary>
    /// EditorPropertiesDialog等でCancel時に元設定を汚さないための複製。
    /// 全プロパティが値型/stringのみのため MemberwiseClone で十分。
    /// </summary>
    public EditorConfig Clone() => (EditorConfig)MemberwiseClone();
}
