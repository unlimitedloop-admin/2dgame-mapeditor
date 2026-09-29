using StageSmith.Core.Models;
using StageSmith.Infrastructure;

namespace StageSmith.Editor.Forms;

/// <summary>
/// ゲーム側の敵定義フォルダと NES パレットの場所を設定するダイアログ。
/// 入力したパスはその場で読み込んで、結果（件数・問題点）を表示する。
/// 設定値の反映は呼び出し側で行う（Undo管理のため、このダイアログはモデルを書き換えない）。
/// </summary>
public sealed class EnemyDefinitionSettingsDialog : Form
{
    private readonly TextBox _directoryTextBox;
    private readonly TextBox _nesPaletteTextBox;
    private readonly TextBox _statusTextBox;

    public string EnemyDefinitionDirectory => _directoryTextBox.Text.Trim();
    public string NesPalettePath => _nesPaletteTextBox.Text.Trim();

    public EnemyDefinitionSettingsDialog(string enemyDefinitionDirectory, string nesPalettePath)
    {
        Text            = "敵定義・パレットの設定";
        StartPosition   = FormStartPosition.CenterParent;
        MinimizeBox     = false;
        MaximizeBox     = false;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        ClientSize      = new Size(560, 330);

        var description = new Label
        {
            Text = "ゲーム側の敵定義（palette_presets など）と NES パレットを読み込み、\n" +
                   "kind の候補・palette の選択肢・色替え表示に使います。",
            Location = new Point(12, 10),
            AutoSize = true,
            ForeColor = Color.DimGray,
        };

        _directoryTextBox = new TextBox { Location = new Point(12, 70), Width = 450, Text = enemyDefinitionDirectory };
        var directoryButton = new Button { Text = "参照...", Location = new Point(470, 68), Size = new Size(78, 26) };

        _nesPaletteTextBox = new TextBox { Location = new Point(12, 122), Width = 450, Text = nesPalettePath };
        var nesPaletteButton = new Button { Text = "参照...", Location = new Point(470, 120), Size = new Size(78, 26) };

        _statusTextBox = new TextBox
        {
            Location = new Point(12, 174),
            Size = new Size(536, 100),
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Vertical,
        };

        var okButton = new Button { Text = "OK", DialogResult = DialogResult.OK, Location = new Point(390, 290), Size = new Size(75, 26) };
        var cancelButton = new Button { Text = "キャンセル", DialogResult = DialogResult.Cancel, Location = new Point(473, 290), Size = new Size(75, 26) };

        Controls.AddRange(
        [
            description,
            new Label { Text = "敵定義フォルダ（例: assets\\data\\enemies）:", Location = new Point(12, 50), AutoSize = true },
            _directoryTextBox, directoryButton,
            new Label { Text = "NES パレット（例: assets\\system\\nes_palette.txt）:", Location = new Point(12, 102), AutoSize = true },
            _nesPaletteTextBox, nesPaletteButton,
            new Label { Text = "読み込み結果:", Location = new Point(12, 154), AutoSize = true },
            _statusTextBox,
            okButton, cancelButton,
        ]);

        AcceptButton = okButton;
        CancelButton = cancelButton;

        directoryButton.Click += (_, _) => BrowseDirectory();
        nesPaletteButton.Click += (_, _) => BrowseNesPalette();
        _directoryTextBox.Leave += (_, _) => { GuessNesPaletteIfEmpty(); UpdateStatus(); };
        _nesPaletteTextBox.Leave += (_, _) => UpdateStatus();

        Shown += (_, _) => UpdateStatus();
    }

    private void BrowseDirectory()
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = "敵定義フォルダを選択",
            UseDescriptionForTitle = true,
        };

        if (Directory.Exists(EnemyDefinitionDirectory))
            dialog.InitialDirectory = EnemyDefinitionDirectory;

        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        _directoryTextBox.Text = dialog.SelectedPath;
        GuessNesPaletteIfEmpty();
        UpdateStatus();
    }

    private void BrowseNesPalette()
    {
        using var dialog = new OpenFileDialog
        {
            Title = "NES パレットを選択",
            Filter = "NES Palette (*.txt)|*.txt|All files (*.*)|*.*",
        };

        var current = NesPalettePath;
        if (File.Exists(current))
            dialog.InitialDirectory = Path.GetDirectoryName(current);

        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        _nesPaletteTextBox.Text = dialog.FileName;
        UpdateStatus();
    }

    /// <summary>NES パレットが未入力なら、敵定義フォルダから場所を推測して埋める。</summary>
    private void GuessNesPaletteIfEmpty()
    {
        if (!string.IsNullOrWhiteSpace(NesPalettePath)) return;

        var guessed = EnemyDefinitionCatalogLoader.GuessNesPalettePath(EnemyDefinitionDirectory);
        if (guessed != null)
            _nesPaletteTextBox.Text = guessed;
    }

    private void UpdateStatus()
    {
        if (string.IsNullOrWhiteSpace(EnemyDefinitionDirectory) && string.IsNullOrWhiteSpace(NesPalettePath))
        {
            _statusTextBox.Text = "未設定（kind・palette は自由入力、色替え表示なし）";
            return;
        }

        var catalog = EnemyDefinitionCatalogLoader.Load(EnemyDefinitionDirectory, NesPalettePath);
        _statusTextBox.Text = Describe(catalog);
    }

    public static string Describe(EnemyDefinitionCatalog catalog)
    {
        var lines = new List<string>
        {
            catalog.HasDefinitions
                ? $"敵定義: {catalog.Definitions.Count()} 件（{string.Join(", ", catalog.Definitions.Select(d => d.Id))}）"
                : "敵定義: 0 件",
            catalog.NesPalette != null ? "NES パレット: 読み込みOK" : "NES パレット: なし（色替え表示なし）",
        };

        lines.AddRange(catalog.Errors.Select(e => "⚠ " + e));
        return string.Join(Environment.NewLine, lines);
    }
}
