using StageSmith.Core.Constants;
using StageSmith.Core.Models;
using StageSmith.Editor.Utilities;

namespace StageSmith.Editor.Forms;

/// <summary>
/// 配置用テンプレート（EntityTemplate）の登録・編集ダイアログ。
/// 入力値の反映は呼び出し側で行う（Undo管理のため、このダイアログはモデルを書き換えない）。
/// </summary>
public sealed class EntityTemplateDialog : Form
{
    private const string Caption = "Entity Template";
    private const int PreviewScale = 2;

    private readonly SpriteSheet _sheet;
    private readonly int _tileIndex;
    private readonly SpriteSheetImageCache _imageCache;

    private readonly TextBox _nameTextBox;
    private readonly ComboBox _typeCombo;
    private readonly ComboBox _kindCombo;
    private readonly TextBox _paletteTextBox;
    private readonly ComboBox _facingCombo;
    private readonly ComboBox _respawnCombo;
    private readonly ComboBox _despawnCombo;

    /// <summary>入力されたテンプレート名（空なら Kind を使う）。</summary>
    public string TemplateName => string.IsNullOrWhiteSpace(_nameTextBox.Text)
        ? _kindCombo.Text.Trim()
        : _nameTextBox.Text.Trim();

    public string EntityType => (string)_typeCombo.SelectedItem!;

    /// <summary>入力されたプロパティ既定値。</summary>
    public EntityProperties Properties => new()
    {
        Kind             = _kindCombo.Text.Trim(),
        Palette          = string.IsNullOrWhiteSpace(_paletteTextBox.Text) ? null : _paletteTextBox.Text.Trim(),
        Facing           = ChoiceItem.GetValue(_facingCombo),
        Respawn          = ChoiceItem.GetValue(_respawnCombo),
        DespawnOffscreen = ChoiceItem.ToBool(ChoiceItem.GetValue(_despawnCombo)),
    };

    /// <param name="existing">編集時は対象テンプレート。新規登録時は null。</param>
    /// <param name="knownKinds">Kind 入力欄の候補（既存テンプレートで使われている kind など）。</param>
    public EntityTemplateDialog(
        SpriteSheet sheet,
        int tileIndex,
        SpriteSheetImageCache imageCache,
        EntityTemplate? existing,
        IEnumerable<string> knownKinds)
    {
        _sheet = sheet;
        _tileIndex = tileIndex;
        _imageCache = imageCache;

        Text            = existing == null ? "テンプレート登録" : "テンプレート編集";
        StartPosition   = FormStartPosition.CenterParent;
        MinimizeBox     = false;
        MaximizeBox     = false;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        AutoSize        = true;
        AutoSizeMode    = AutoSizeMode.GrowAndShrink;
        Padding         = new Padding(10);

        //========================
        // 入力欄
        //========================
        _nameTextBox = new TextBox { Width = 220 };

        _typeCombo = CreateDropDownList();
        _typeCombo.Items.Add(EntityConstants.TypeEnemy);

        _kindCombo = new ComboBox { Width = 220, DropDownStyle = ComboBoxStyle.DropDown };
        foreach (var kind in knownKinds.Where(k => !string.IsNullOrWhiteSpace(k)).Distinct().Order())
            _kindCombo.Items.Add(kind);

        _paletteTextBox = new TextBox { Width = 220, PlaceholderText = $"(既定: {EntityConstants.DefaultPalette})" };

        _facingCombo = CreateDropDownList();
        ChoiceItem.FillFacing(_facingCombo);

        _respawnCombo = CreateDropDownList();
        ChoiceItem.FillRespawn(_respawnCombo);

        _despawnCombo = CreateDropDownList();
        ChoiceItem.FillDespawn(_despawnCombo);

        var fields = new TableLayoutPanel
        {
            AutoSize = true,
            ColumnCount = 2,
            Dock = DockStyle.Fill,
        };

        AddRow(fields, "名前:", _nameTextBox);
        AddRow(fields, "type:", _typeCombo);
        AddRow(fields, "kind (必須):", _kindCombo);
        AddRow(fields, "palette:", _paletteTextBox);
        AddRow(fields, "facing:", _facingCombo);
        AddRow(fields, "respawn:", _respawnCombo);
        AddRow(fields, "despawnOffscreen:", _despawnCombo);

        //========================
        // プレビュー
        //========================
        var preview = new DoubleBufferedPreview(this)
        {
            Size = new Size(
                Math.Max(64, sheet.TileWidth * PreviewScale + 8),
                Math.Max(64, sheet.TileHeight * PreviewScale + 8)),
            Margin = new Padding(0, 3, 12, 3),
        };

        var previewCaption = new Label
        {
            Text = $"{sheet.Name}\n#{tileIndex}",
            AutoSize = true,
            ForeColor = Color.DimGray,
        };

        var previewColumn = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.TopDown,
            AutoSize = true,
        };
        previewColumn.Controls.Add(preview);
        previewColumn.Controls.Add(previewCaption);

        //========================
        // ボタン
        //========================
        var okButton = new Button { Text = existing == null ? "登録" : "更新", DialogResult = DialogResult.OK, Size = new Size(75, 26) };
        var cancelButton = new Button { Text = "キャンセル", DialogResult = DialogResult.Cancel, Size = new Size(75, 26) };

        var buttons = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.RightToLeft,
            AutoSize = true,
            Dock = DockStyle.Fill,
        };
        buttons.Controls.Add(cancelButton);
        buttons.Controls.Add(okButton);

        var note = new Label
        {
            Text = existing == null
                ? "値は配置時の既定値としてコピーされます。配置後に個別変更できます。"
                : "変更はこれから配置するものにだけ反映されます（配置済みのものは変わりません）。",
            AutoSize = true,
            MaximumSize = new Size(360, 0),
            ForeColor = Color.DimGray,
            Margin = new Padding(3, 8, 3, 3),
        };

        var root = new TableLayoutPanel
        {
            AutoSize = true,
            ColumnCount = 2,
            RowCount = 3,
        };
        root.Controls.Add(previewColumn, 0, 0);
        root.Controls.Add(fields, 1, 0);
        root.Controls.Add(note, 0, 1);
        root.SetColumnSpan(note, 2);
        root.Controls.Add(buttons, 0, 2);
        root.SetColumnSpan(buttons, 2);

        Controls.Add(root);

        AcceptButton = okButton;
        CancelButton = cancelButton;

        //========================
        // 初期値
        //========================
        var initial = existing?.Properties ?? new EntityProperties();

        _nameTextBox.Text = existing?.Name ?? string.Empty;
        _nameTextBox.PlaceholderText = "(空欄なら kind と同じ)";
        _typeCombo.SelectedItem = existing?.Type ?? EntityConstants.TypeEnemy;
        if (_typeCombo.SelectedIndex < 0) _typeCombo.SelectedIndex = 0;
        _kindCombo.Text = initial.Kind;
        _paletteTextBox.Text = initial.Palette ?? string.Empty;
        ChoiceItem.Select(_facingCombo, initial.Facing);
        ChoiceItem.Select(_respawnCombo, initial.Respawn);
        ChoiceItem.Select(_despawnCombo, ChoiceItem.FromBool(initial.DespawnOffscreen));

        FormClosing += (_, e) =>
        {
            if (DialogResult != DialogResult.OK) return;

            if (string.IsNullOrWhiteSpace(_kindCombo.Text))
            {
                MessageBox.Show(this, "kind（敵定義の id）を入力してください。", Caption,
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                e.Cancel = true;
                _kindCombo.Focus();
            }
        };

        Shown += (_, _) =>
        {
            if (string.IsNullOrEmpty(_kindCombo.Text)) _kindCombo.Focus();
            else _nameTextBox.Focus();
        };
    }

    private static ComboBox CreateDropDownList()
        => new() { Width = 220, DropDownStyle = ComboBoxStyle.DropDownList };

    private static void AddRow(TableLayoutPanel table, string label, Control control)
    {
        var row = table.RowCount++;
        table.Controls.Add(new Label
        {
            Text = label,
            AutoSize = true,
            Anchor = AnchorStyles.Left,
            Margin = new Padding(3, 6, 3, 3),
        }, 0, row);
        table.Controls.Add(control, 1, row);
    }

    private void DrawPreview(Graphics g, Rectangle bounds)
    {
        g.Clear(Color.FromArgb(235, 235, 235));
        g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
        g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;

        var w = _sheet.TileWidth * PreviewScale;
        var h = _sheet.TileHeight * PreviewScale;
        var dst = new Rectangle((bounds.Width - w) / 2, (bounds.Height - h) / 2, w, h);

        if (!_imageCache.DrawTile(g, _sheet, _tileIndex, dst))
        {
            using var missingPen = new Pen(Color.OrangeRed) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dash };
            g.DrawRectangle(missingPen, dst.X, dst.Y, dst.Width - 1, dst.Height - 1);
        }

        // 足元中心（.def の position が指す点）＝コマの下辺中央
        var footX = dst.X + dst.Width / 2;
        var footY = dst.Bottom - 1;
        using var footPen = new Pen(Color.FromArgb(200, Color.Red));
        g.DrawLine(footPen, footX - 4, footY, footX + 4, footY);
        g.DrawLine(footPen, footX, footY - 4, footX, footY);
    }

    private sealed class DoubleBufferedPreview : Panel
    {
        public DoubleBufferedPreview(EntityTemplateDialog owner)
        {
            DoubleBuffered = true;
            BorderStyle = BorderStyle.FixedSingle;
            Paint += (_, e) => owner.DrawPreview(e.Graphics, ClientRectangle);
        }
    }
}
