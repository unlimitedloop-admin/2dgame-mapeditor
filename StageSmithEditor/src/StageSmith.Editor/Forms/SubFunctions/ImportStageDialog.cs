using StageSmith.Core.Models;

namespace StageSmith.Editor.Forms;

/// <summary>インポート元プロジェクトのステージ一覧から1件選択させるダイアログ。</summary>
public sealed class ImportStageDialog : Form
{
    private readonly ListBox _listBox;

    public Stage? SelectedStage => (_listBox.SelectedItem as StageListItem)?.Stage;

    public ImportStageDialog(IEnumerable<Stage> stages)
    {
        Text            = "ステージを選択";
        StartPosition   = FormStartPosition.CenterParent;
        ClientSize      = new Size(360, 320);
        MinimizeBox     = false;
        MaximizeBox     = false;
        FormBorderStyle = FormBorderStyle.FixedDialog;

        _listBox = new ListBox { Dock = DockStyle.Fill };

        foreach (var stage in stages)
        {
            var text = $"{stage.Name}  [{stage.Key}]  #{stage.StageNumber}  ({stage.Pages.Count} pages)";
            _listBox.Items.Add(new StageListItem(stage, text));
        }

        if (_listBox.Items.Count > 0)
            _listBox.SelectedIndex = 0;

        _listBox.DoubleClick += (_, _) => AcceptSelection();

        var buttonPanel = new Panel { Dock = DockStyle.Bottom, Height = 40 };

        var okButton = new Button
        {
            Text         = "OK",
            DialogResult = DialogResult.OK,
            Location     = new Point(ClientSize.Width - 170, 8),
            Size         = new Size(75, 26),
        };
        okButton.Click += (_, _) => AcceptSelection();

        var cancelButton = new Button
        {
            Text         = "Cancel",
            DialogResult = DialogResult.Cancel,
            Location     = new Point(ClientSize.Width - 85, 8),
            Size         = new Size(75, 26),
        };

        buttonPanel.Controls.Add(okButton);
        buttonPanel.Controls.Add(cancelButton);

        Controls.Add(_listBox);
        Controls.Add(buttonPanel);

        AcceptButton = okButton;
        CancelButton = cancelButton;
    }

    private void AcceptSelection()
    {
        if (_listBox.SelectedItem == null) return;
        DialogResult = DialogResult.OK;
        Close();
    }

    private sealed class StageListItem(Stage stage, string text)
    {
        public Stage Stage { get; } = stage;
        public override string ToString() => text;
    }
}
