namespace StageSmith.Editor;

public partial class MainForm
{
    private StatusStrip _statusStrip = null!;
    private ToolStripStatusLabel _statusLabel = null!;

    private void InitializeStatusBar()
    {
        _statusStrip = new StatusStrip
        {
            Dock = DockStyle.Bottom
        };

        _statusLabel = new ToolStripStatusLabel
        {
            Text = "",
            Spring = true,
            TextAlign = ContentAlignment.MiddleLeft
        };

        _statusStrip.Items.Add(_statusLabel);
        Controls.Add(_statusStrip);
    }

    /// <summary>
    /// ステータスバーへ操作完了メッセージを表示する。
    /// </summary>
    private void ShowStatusMessage(string message)
    {
        _statusLabel.Text = message;
    }

    /// <summary>
    /// フルパスを、末尾のファイル名を優先して指定文字数以内に短縮する。
    /// 例: C:\Users\youvi\Documents\Projects\NewProject.sseproj (maxLength=40)
    ///  → ...\Documents\Projects\NewProject.sseproj
    /// </summary>
    private static string TruncatePathForStatus(string fullPath, int maxLength = 60)
    {
        if (fullPath.Length <= maxLength)
            return fullPath;

        // 末尾から maxLength - 3（"..."分）だけ切り出す
        var tailLength = maxLength - 3;
        var tail = fullPath[^tailLength..];

        return "..." + tail;
    }
}
