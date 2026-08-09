using StageSmith.Core.Constants;
using StageSmith.Core.Models;

namespace StageSmith.Editor.Forms;

/// <summary>
/// エディタプロパティ（拡張機能設定）編集ダイアログ。
/// Cancel時に元の設定を汚さないよう、渡された EditorConfig はクローンして編集する。
/// OK確定時のみ Result プロパティ経由で呼び出し元へ反映済みの設定を返す。
/// </summary>
public sealed class EditorPropertiesDialog : Form
{
    public EditorConfig Result { get; }

    // ---- General タブ ----
    private readonly RadioButton _hexRadio;
    private readonly RadioButton _decimalRadio;
    private readonly CheckBox _keepSelectedTileCheckBox;
    private readonly CheckBox _showNodePreviewCheckBox;
    private readonly NumericUpDown _defaultClearTileIdUpDown;

    // ---- Project タブ ----
    private readonly CheckBox _useStageSubFolderCheckBox;
    private readonly CheckBox _useProjectSubDirectoryCheckBox;
    private readonly TextBox _defaultSaveDirectoryTextBox;

    // ---- File タブ ----
    private readonly CheckBox _useStageDirectoryForExportCheckBox;

    public EditorPropertiesDialog(EditorConfig currentConfig)
    {
        Result = currentConfig.Clone();

        Text            = "Options";
        StartPosition   = FormStartPosition.CenterParent;
        ClientSize      = new Size(420, 340);
        MinimizeBox     = false;
        MaximizeBox     = false;
        FormBorderStyle = FormBorderStyle.FixedDialog;

        var tabControl = new TabControl
        {
            Location = new Point(12, 12),
            Size     = new Size(396, 260),
        };

        var generalTab = new TabPage("General");
        var projectTab = new TabPage("Project");
        var fileTab    = new TabPage("File");
        tabControl.TabPages.Add(generalTab);
        tabControl.TabPages.Add(projectTab);
        tabControl.TabPages.Add(fileTab);

        // ============================================
        // General タブ
        // ============================================
        var displayFormatGroup = new GroupBox
        {
            Text     = "タイル番号の表示形式",
            Location = new Point(12, 12),
            Size     = new Size(360, 70),
        };

        _hexRadio = new RadioButton
        {
            Text     = "16進数",
            Location = new Point(16, 25),
            AutoSize = true,
            Checked  = Result.NumberDisplayFormat == NumberDisplayFormat.Hex,
        };

        _decimalRadio = new RadioButton
        {
            Text     = "10進数",
            Location = new Point(16, 45),
            AutoSize = true,
            Checked  = Result.NumberDisplayFormat == NumberDisplayFormat.Decimal,
        };

        displayFormatGroup.Controls.Add(_hexRadio);
        displayFormatGroup.Controls.Add(_decimalRadio);

        _keepSelectedTileCheckBox = new CheckBox
        {
            Text     = "ページ移動時に選択中のタイルを維持する",
            Location = new Point(16, 92),
            AutoSize = true,
            Checked  = Result.KeepSelectedTileOnPageChange,
        };

        _showNodePreviewCheckBox = new CheckBox
        {
            Text     = "ページノードエディタでタイルプレビューを表示する",
            Location = new Point(16, 116),
            AutoSize = true,
            Checked  = Result.ShowNodePreview,
        };

        var clearTileLabel = new Label
        {
            Text     = "タイルクリア時のデフォルトタイル番号 (0-255):",
            Location = new Point(16, 154),
            AutoSize = true,
        };

        _defaultClearTileIdUpDown = new NumericUpDown
        {
            Location = new Point(16, 174),
            Width    = 80,
            Minimum  = 0,
            Maximum  = 255,
            Value    = Result.DefaultClearTileId,
        };

        void UpdateClearTileIdFormat()
        {
            var hex = _hexRadio.Checked;
            _defaultClearTileIdUpDown.Hexadecimal = hex;
            clearTileLabel.Text = hex
                ? "タイルクリア時のデフォルトタイル番号 (00-FF):"
                : "タイルクリア時のデフォルトタイル番号 (0-255):";
        }

        _hexRadio.CheckedChanged += (_, _) => UpdateClearTileIdFormat();
        _decimalRadio.CheckedChanged += (_, _) => UpdateClearTileIdFormat();
        UpdateClearTileIdFormat();

        generalTab.Controls.Add(displayFormatGroup);
        generalTab.Controls.Add(_keepSelectedTileCheckBox);
        generalTab.Controls.Add(_showNodePreviewCheckBox);
        generalTab.Controls.Add(clearTileLabel);
        generalTab.Controls.Add(_defaultClearTileIdUpDown);

        // ============================================
        // Project タブ
        // ============================================
        _useStageSubFolderCheckBox = new CheckBox
        {
            Text     = "ステージごとにサブフォルダを分ける",
            Location = new Point(16, 16),
            AutoSize = true,
            Checked  = Result.UseStageSubFolder,
        };

        _useProjectSubDirectoryCheckBox = new CheckBox
        {
            Text     = "プロジェクトをサブディレクトリ化する",
            Location = new Point(16, 44),
            AutoSize = true,
            Checked  = Result.UseProjectSubDirectory,
        };

        var saveDirLabel = new Label
        {
            Text     = "プロジェクトのデフォルト保存先:",
            Location = new Point(16, 84),
            AutoSize = true,
        };

        _defaultSaveDirectoryTextBox = new TextBox
        {
            Location = new Point(16, 104),
            Width    = 270,
            Text     = Result.DefaultProjectSaveDirectory ?? string.Empty,
        };

        var browseButton = new Button
        {
            Text     = "参照...",
            Location = new Point(292, 103),
            Size     = new Size(64, 24),
        };

        browseButton.Click += (_, _) =>
        {
            using var folderDialog = new FolderBrowserDialog
            {
                Description = "プロジェクトのデフォルト保存先を選択",
                SelectedPath = _defaultSaveDirectoryTextBox.Text,
            };

            if (folderDialog.ShowDialog(this) == DialogResult.OK)
            {
                _defaultSaveDirectoryTextBox.Text = folderDialog.SelectedPath;
            }
        };

        projectTab.Controls.Add(_useStageSubFolderCheckBox);
        projectTab.Controls.Add(_useProjectSubDirectoryCheckBox);
        projectTab.Controls.Add(saveDirLabel);
        projectTab.Controls.Add(_defaultSaveDirectoryTextBox);
        projectTab.Controls.Add(browseButton);

        // ============================================
        // File タブ
        // ============================================
        _useStageDirectoryForExportCheckBox = new CheckBox
        {
            Text     = "バイナリ出力先の初期フォルダをステージファイルの保存先にする",
            Location = new Point(16, 16),
            AutoSize = true,
            Checked  = Result.UseStageDirectoryForExport,
        };

        fileTab.Controls.Add(_useStageDirectoryForExportCheckBox);

        // ============================================
        // OK / Cancel
        // ============================================
        var okButton = new Button
        {
            Text         = "OK",
            DialogResult = DialogResult.OK,
            Location     = new Point(ClientSize.Width - 170, 284),
            Size         = new Size(75, 26),
        };

        var cancelButton = new Button
        {
            Text         = "キャンセル",
            DialogResult = DialogResult.Cancel,
            Location     = new Point(ClientSize.Width - 85, 284),
            Size         = new Size(75, 26),
        };

        okButton.Click += (_, _) => CommitToResult();

        Controls.Add(tabControl);
        Controls.Add(okButton);
        Controls.Add(cancelButton);

        AcceptButton = okButton;
        CancelButton = cancelButton;
    }

    /// <summary>
    /// OK確定時、各コントロールの入力値を Result（クローン済みConfig）へ書き戻す。
    /// </summary>
    private void CommitToResult()
    {
        Result.NumberDisplayFormat = _hexRadio.Checked
            ? NumberDisplayFormat.Hex
            : NumberDisplayFormat.Decimal;

        Result.KeepSelectedTileOnPageChange = _keepSelectedTileCheckBox.Checked;
        Result.ShowNodePreview = _showNodePreviewCheckBox.Checked;
        Result.DefaultClearTileId = (byte)_defaultClearTileIdUpDown.Value;

        Result.UseStageSubFolder = _useStageSubFolderCheckBox.Checked;
        Result.UseProjectSubDirectory = _useProjectSubDirectoryCheckBox.Checked;
        Result.DefaultProjectSaveDirectory = _defaultSaveDirectoryTextBox.Text.Trim();
        Result.UseStageDirectoryForExport = _useStageDirectoryForExportCheckBox.Checked;
    }
}
