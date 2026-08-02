namespace StageSmith.Editor.Forms;

/// <summary>
/// タグアイコンの選択ダイアログ。
/// デフォルト素材（Assets/TagIcons/配下）から選ぶか、カスタム画像を参照するかを選べる。
/// SelectedSourcePath は「コピー元」の絶対パス。呼び出し元がプロジェクトのTagIcons/へコピーする。
/// OKかつ SelectedSourcePath が null の場合は「アイコンを設定しない」が選ばれたことを意味する。
/// </summary>
public sealed class TagIconPickerDialog : Form
{
    private static readonly string[] DefaultIconFileNames =
    [
        "icons8-high-priority-48.png",
        "icons8-todoリスト-48.png",
        "icons8-タグ-48.png",
    ];

    public string? SelectedSourcePath { get; private set; }

    public TagIconPickerDialog()
    {
        Text            = "タグアイコンを選択";
        StartPosition   = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MinimizeBox     = false;
        MaximizeBox     = false;
        ClientSize      = new Size(360, 260);

        var defaultsGroup = new GroupBox
        {
            Text = "デフォルト素材",
            Dock = DockStyle.Top,
            Height = 110,
        };

        var flow = new FlowLayoutPanel
        {
            Dock          = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents  = true,
            Padding       = new Padding(8),
        };

        foreach (var fileName in DefaultIconFileNames)
        {
            var path = Path.Combine(AppContext.BaseDirectory, "Assets", "TagIcons", fileName);
            if (!File.Exists(path)) continue;

            var button = new Button
            {
                Width  = 56,
                Height = 56,
                Margin = new Padding(4),
                ImageAlign = ContentAlignment.MiddleCenter,
            };

            try
            {
                using var src = Image.FromFile(path);
                button.Image = new Bitmap(src, new Size(40, 40));
            }
            catch
            {
                button.Text = "?";
            }

            button.Click += (_, _) =>
            {
                SelectedSourcePath = path;
                DialogResult = DialogResult.OK;
                Close();
            };

            flow.Controls.Add(button);
        }

        defaultsGroup.Controls.Add(flow);

        var hintLabel = new Label
        {
            Text = "推奨: 正方形・64×64px程度の画像（表示時は自動で縮小されます）",
            Dock = DockStyle.Top,
            Height = 20,
            ForeColor = SystemColors.GrayText,
            Font = new Font("Yu Gothic UI", 8f),
        };

        var browseButton = new Button
        {
            Text   = "カスタム画像を参照...",
            Dock   = DockStyle.Top,
            Height = 32,
        };

        browseButton.Click += (_, _) =>
        {
            using var dialog = new OpenFileDialog
            {
                Title  = "タグアイコン画像を選択",
                Filter = "画像ファイル (*.png;*.bmp)|*.png;*.bmp",
            };

            if (dialog.ShowDialog(this) != DialogResult.OK) return;

            SelectedSourcePath = dialog.FileName;
            DialogResult = DialogResult.OK;
            Close();
        };

        var clearButton = new Button
        {
            Text   = "アイコンを設定しない",
            Dock   = DockStyle.Top,
            Height = 32,
        };

        clearButton.Click += (_, _) =>
        {
            SelectedSourcePath = null;
            DialogResult = DialogResult.OK;
            Close();
        };

        var cancelButton = new Button
        {
            Text         = "キャンセル",
            Dock         = DockStyle.Bottom,
            Height       = 32,
            DialogResult = DialogResult.Cancel,
        };

        Controls.Add(defaultsGroup);
        Controls.Add(hintLabel);
        Controls.Add(browseButton);
        Controls.Add(clearButton);
        Controls.Add(cancelButton);

        CancelButton = cancelButton;
    }
}
