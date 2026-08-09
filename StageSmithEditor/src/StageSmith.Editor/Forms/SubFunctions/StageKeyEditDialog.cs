namespace StageSmith.Editor;

/// <summary>
/// Stage.Key（将来の.def出力等で使う識別子）を編集するための小さなダイアログ。
///
/// NOTE: Key は現時点でエディタ内の他機能とは連動しない、こだわるユーザー向けの
/// 補助的なプロパティという位置づけ。そのため専用の目立つ画面は用意せず、
/// StageExplorerの右クリックメニューから開く控えめな小ダイアログとする。
/// </summary>
public sealed class StageKeyEditDialog : Form
{
    /// <summary>OKで確定したKey。キャンセル時は参照しないこと。</summary>
    public string ResultKey { get; private set; } = string.Empty;

    private readonly TextBox _txtKey;

    public StageKeyEditDialog(string stageName, string currentKey)
    {
        Text            = $"Key を設定  -  {stageName}";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition   = FormStartPosition.CenterParent;
        MaximizeBox     = false;
        MinimizeBox     = false;
        ShowInTaskbar   = false;
        Size            = new Size(360, 186);

        var layout = new TableLayoutPanel
        {
            Dock        = DockStyle.Fill,
            ColumnCount = 1,
            RowCount    = 3,
            Padding     = new Padding(12),
        };
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var infoLabel = new Label
        {
            Text      = "ゲーム側の識別子として将来使う想定の任意項目です。\n" +
                        "エディタの他機能とは連動しません（空欄可）。",
            AutoSize  = false,
            Dock      = DockStyle.Fill,
            Height    = 40,
            ForeColor = SystemColors.GrayText,
            Font      = new Font("Yu Gothic UI", 8.5f),
        };

        _txtKey = new TextBox
        {
            Text   = currentKey,
            Dock   = DockStyle.Fill,
            Font   = new Font("Yu Gothic UI", 9f),
        };
        _txtKey.KeyDown += (_, e) =>
        {
            if (e.KeyCode != Keys.Enter) return;
            e.SuppressKeyPress = true;
            OnOkClick(null, EventArgs.Empty);
        };

        var buttonPanel = new FlowLayoutPanel
        {
            Dock          = DockStyle.Bottom,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents  = false,
            Padding       = new Padding(0, 8, 0, 0),
        };

        var cancelButton = new Button { Text = "キャンセル", Width = 90, Height = 28, DialogResult = DialogResult.Cancel };
        var okButton     = new Button { Text = "OK",         Width = 90, Height = 28, DialogResult = DialogResult.None };
        okButton.Click += OnOkClick;

        buttonPanel.Controls.AddRange([cancelButton, okButton]);

        layout.Controls.Add(infoLabel, 0, 0);
        layout.Controls.Add(_txtKey,   0, 1);
        layout.Controls.Add(buttonPanel, 0, 2);

        Controls.Add(layout);
        AcceptButton = okButton;
        CancelButton = cancelButton;
    }

    private void OnOkClick(object? sender, EventArgs e)
    {
        ResultKey = _txtKey.Text.Trim();
        DialogResult = DialogResult.OK;
        Close();
    }
}
