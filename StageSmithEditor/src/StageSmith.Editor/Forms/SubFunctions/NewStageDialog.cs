namespace StageSmith.Editor.Forms;

/// <summary>
/// 新規ステージ作成時にステージ名を入力させるダイアログ
/// </summary>
public sealed class NewStageDialog : Form
{
    private readonly TextBox _nameTextBox;

    public string StageName => _nameTextBox.Text.Trim();

    public NewStageDialog(string defaultName)
    {
        Text            = "New Stage";
        StartPosition   = FormStartPosition.CenterParent;
        ClientSize      = new Size(320, 110);
        MinimizeBox     = false;
        MaximizeBox     = false;
        FormBorderStyle = FormBorderStyle.FixedDialog;

        var label = new Label
        {
            Text     = "ステージ名:",
            Location = new Point(12, 15),
            AutoSize = true,
        };

        _nameTextBox = new TextBox
        {
            Text     = defaultName,
            Location = new Point(12, 35),
            Width    = 296,
        };

        var createButton = new Button
        {
            Text         = "作成",
            DialogResult = DialogResult.OK,
            Location     = new Point(ClientSize.Width - 170, 70),
            Size         = new Size(75, 26),
        };

        var cancelButton = new Button
        {
            Text         = "キャンセル",
            DialogResult = DialogResult.Cancel,
            Location     = new Point(ClientSize.Width - 85, 70),
            Size         = new Size(75, 26),
        };

        Controls.Add(label);
        Controls.Add(_nameTextBox);
        Controls.Add(createButton);
        Controls.Add(cancelButton);

        AcceptButton = createButton;
        CancelButton = cancelButton;

        // 名前未入力のまま「作成」しようとした場合はダイアログを閉じさせない
        FormClosing += (_, e) =>
        {
            if (DialogResult != DialogResult.OK) return;

            if (string.IsNullOrWhiteSpace(_nameTextBox.Text))
            {
                MessageBox.Show(
                    this,
                    "ステージ名を入力してください。",
                    "New Stage",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                e.Cancel = true;
                _nameTextBox.Focus();
            }
        };

        Shown += (_, _) =>
        {
            _nameTextBox.Focus();
            _nameTextBox.SelectAll();
        };
    }
}
