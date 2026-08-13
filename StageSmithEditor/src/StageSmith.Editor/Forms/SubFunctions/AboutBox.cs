using System.Diagnostics;
using System.Reflection;

namespace StageSmith.Editor;

/// <summary>
/// Help &gt; About StageSmith Editor で表示するダイアログ。
/// アプリ情報に加え、サードパーティ素材・ライブラリのクレジット表記を掲載する。
/// NOTE: Icons8のアイコン素材は無償利用条件として「ウェブサイトへのリンク表示」を
/// 求められている。ステータスバーはDPI設定・ウィンドウ幅により表示が欠ける
/// 問題が確認されたため、常に固定表示領域を確保できる本ダイアログへ集約した。
/// （参考：Icons8公式FAQでもリンクの設置場所自体は指定されておらず、
/// ユーザーから視認できる場所であればよいとされている）
/// </summary>
public sealed class AboutBox : Form
{
    private const string Icons8Url = "https://icons8.jp/";
    private const string DockPanelSuiteUrl = "https://github.com/dockpanelsuite/dockpanelsuite";

    public AboutBox()
    {
        Text = "About StageSmith Editor";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        ClientSize = new Size(380, 280);

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            Padding = new Padding(16),
            AutoSize = true
        };

        var titleLabel = new Label
        {
            Text = "StageSmith Editor",
            Font = new Font(Font.FontFamily, 14f, FontStyle.Bold),
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 4)
        };

        var versionLabel = new Label
        {
            Text = $"Version {GetDisplayVersion()}",
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 12)
        };

        var copyrightLabel = new Label
        {
            Text = "© 2026 u7",
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 16)
        };

        var creditsHeaderLabel = new Label
        {
            Text = "Credits",
            Font = new Font(Font.FontFamily, 9f, FontStyle.Bold),
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 4)
        };

        // DockPanel Suite クレジット（リンク付き）
        var dockPanelCreditLabel = new LinkLabel
        {
            Text = "DockPanel Suite (WeifenLuo) - MIT License",
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 4)
        };
        dockPanelCreditLabel.LinkClicked += (_, _) => OpenUrl(DockPanelSuiteUrl);

        // Icons8 クレジット（リンク付き／無償利用条件）
        var iconsCreditLabel = new LinkLabel
        {
            Text = "Icons by Icons8",
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 16)
        };
        iconsCreditLabel.LinkClicked += (_, _) => OpenUrl(Icons8Url);

        var okButton = new Button
        {
            Text = "OK",
            DialogResult = DialogResult.OK,
            AutoSize = true,
            Anchor = AnchorStyles.Right,
            Margin = new Padding(0, 8, 0, 0)
        };

        layout.Controls.Add(titleLabel);
        layout.Controls.Add(versionLabel);
        layout.Controls.Add(copyrightLabel);
        layout.Controls.Add(creditsHeaderLabel);
        layout.Controls.Add(dockPanelCreditLabel);
        layout.Controls.Add(iconsCreditLabel);
        layout.Controls.Add(okButton);

        Controls.Add(layout);

        AcceptButton = okButton;
        CancelButton = okButton;
    }

    /// <summary>
    /// アセンブリ情報からバージョン文字列を取得する。
    /// csproj の &lt;Version&gt; 値と同期するため、ハードコードは行わない。
    /// </summary>
    private static string GetDisplayVersion()
    {
        var version = Assembly.GetExecutingAssembly().GetName().Version;
        return version is null ? "unknown" : $"{version.Major}.{version.Minor}.{version.Build}";
    }

    private static void OpenUrl(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"リンクを開けませんでした。\n{ex.Message}",
                "エラー",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }
    }
}
