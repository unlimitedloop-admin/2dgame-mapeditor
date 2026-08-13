using StageSmith.Core.Constants;
using StageSmith.Editor.Utilities;

namespace StageSmith.Editor.Forms;

/// <summary>
/// ページ番号を直接入力して該当ページへジャンプするための軽量ダイアログ。
/// </summary>
public sealed class JumpPageDialog : Form
{
    private readonly NumericUpDown _pageNumeric;

    /// <summary>1始まりのページ番号（表示用の "1 / 4" と同じ基準）。</summary>
    public int SelectedPageNumber => (int)_pageNumeric.Value;

    public JumpPageDialog(int currentPageNumber, int maxPageNumber, NumberDisplayFormat format = NumberDisplayFormat.Decimal)
    {
        Text            = "ページジャンプ";
        StartPosition   = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MinimizeBox     = false;
        MaximizeBox     = false;
        ClientSize      = new Size(240, 110);

        var clampedMax = Math.Max(1, maxPageNumber);

        var label = new Label
        {
            Text     = $"ページ番号を入力 (1〜{NumberFormatHelper.FormatByte(clampedMax, format)}):",
            AutoSize = true,
            Location = new Point(12, 15)
        };

        _pageNumeric = new NumericUpDown
        {
            Minimum     = 1,
            Maximum     = clampedMax,
            Value       = Math.Clamp(currentPageNumber, 1, clampedMax),
            Location    = new Point(12, 40),
            Width       = 100,
            Hexadecimal = format == NumberDisplayFormat.Hex
        };

        var okButton = new Button
        {
            Text         = "OK",
            DialogResult = DialogResult.OK,
            Location     = new Point(56, 75),
            Size         = new Size(75, 26)
        };

        var cancelButton = new Button
        {
            Text         = "Cancel",
            DialogResult = DialogResult.Cancel,
            Location     = new Point(140, 75),
            Size         = new Size(75, 26)
        };

        Controls.Add(label);
        Controls.Add(_pageNumeric);
        Controls.Add(okButton);
        Controls.Add(cancelButton);

        AcceptButton = okButton;
        CancelButton = cancelButton;

        Shown += (_, _) =>
        {
            _pageNumeric.Focus();
            _pageNumeric.Select(0, _pageNumeric.Text.Length); // 即入力できるように全選択
        };
    }
}
