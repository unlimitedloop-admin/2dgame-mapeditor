namespace StageSmith.Editor.Controls;

public class BookmarkEditDialog : Form
{
    private readonly TextBox _labelBox;
    private readonly TextBox _descriptionBox;

    public string Label => _labelBox.Text.Trim();
    public string Description => _descriptionBox.Text.Trim();

    public BookmarkEditDialog(string label, string description)
    {
        Text = "Edit Bookmark";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MinimizeBox = false;
        MaximizeBox = false;
        ClientSize = new Size(320, 160);

        var labelCaption = new Label { Text = "Label", Location = new Point(12, 12), AutoSize = true };
        _labelBox = new TextBox { Location = new Point(12, 32), Width = 296, Text = label };

        var descCaption = new Label { Text = "Description", Location = new Point(12, 64), AutoSize = true };
        _descriptionBox = new TextBox
        {
            Location = new Point(12, 84),
            Width = 296,
            Height = 40,
            Multiline = true,
            Text = description
        };

        var okButton = new Button
        {
            Text = "OK",
            DialogResult = DialogResult.OK,
            Location = new Point(152, 132)
        };

        var cancelButton = new Button
        {
            Text = "Cancel",
            DialogResult = DialogResult.Cancel,
            Location = new Point(233, 132)
        };

        Controls.Add(labelCaption);
        Controls.Add(_labelBox);
        Controls.Add(descCaption);
        Controls.Add(_descriptionBox);
        Controls.Add(okButton);
        Controls.Add(cancelButton);

        AcceptButton = okButton;
        CancelButton = cancelButton;
    }
}
