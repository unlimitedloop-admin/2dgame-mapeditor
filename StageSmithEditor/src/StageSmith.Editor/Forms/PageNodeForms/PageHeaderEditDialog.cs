using StageSmith.Core.Constants;
using StageSmith.Core.Models;
using StageSmith.Editor.Utilities;

namespace StageSmith.Editor;

/// <summary>
/// ページヘッダーの接続・スクロール設定を編集するダイアログ。
/// ノードエディタのノードダブルクリックで開く。
/// OKで閉じた時点でPageHeaderに反映する。
/// </summary>
public class PageHeaderEditDialog : Form
{
    //========================
    // 入力データ
    //========================
    private readonly Page _page;

    //========================
    // 結果
    //========================
    /// <summary>OKで確定したPageHeader。キャンセル時は元の値のまま。</summary>
    public PageHeader ResultHeader { get; private set; }

    /// <summary>OKで確定した有効フラグ。キャンセル時は元の値のまま。</summary>
    public bool ResultEnable { get; private set; }

    /// <summary>OKで確定した備考。キャンセル時は元の値のまま。</summary>
    public string ResultRemarks { get; private set; } = string.Empty;

    //========================
    // 接続先TextBox
    //========================
    private readonly TextBox _txtLeft, _txtRight, _txtUp, _txtDown, _txtBack, _txtFront;

    //========================
    // スクロール種別ComboBox
    //========================
    private readonly ComboBox _cmbScrollLeft, _cmbScrollRight, _cmbScrollUp, _cmbScrollDown;

    //========================
    // その他
    //========================
    private readonly CheckBox _chkEnable;
    private readonly CheckBox _chkContinuePoint;
    private readonly CheckBox _chkNoScrollBack;
    private readonly CheckBox _chkPostEffects;
    private readonly CheckBox _chkDarkness;
    private readonly CheckBox _chkWind;
    private readonly CheckBox _chkGravityModifier;
    private readonly TextBox  _txtRemarks;
    private readonly Label    _lblZValue;
    private readonly NumberDisplayFormat _format;

    //========================
    // 初期化
    //========================
    public PageHeaderEditDialog(Page page, NumberDisplayFormat format)
    {
        _page        = page;
        _format      = format;
        ResultHeader = page.Header;

        Text            = $"ページヘッダー編集  -  Room ID: {NumberFormatHelper.FormatByte(page.Header.RoomId, format)}";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition   = FormStartPosition.CenterParent;
        MaximizeBox     = false;
        MinimizeBox     = false;
        Size            = new Size(420, 560);

        var layout = new TableLayoutPanel
        {
            Dock        = DockStyle.Fill,
            ColumnCount = 1,
            RowCount    = 5,
            Padding     = new Padding(12),
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));   // Z座標（読取専用）
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 148));  // 接続先
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 120));  // スクロール種別
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));   // その他
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));   // ボタン

        // --- Z座標（読取専用） ---
        var zPanel = new FlowLayoutPanel
        {
            Dock          = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents  = false,
        };
        zPanel.Controls.Add(new Label
        {
            Text      = "Z座標（読取専用）:",
            AutoSize  = true,
            Font      = new Font("Yu Gothic UI", 9f),
            Padding   = new Padding(0, 5, 8, 0),
        });
        _lblZValue = new Label
        {
            Text      = page.Header.Z.ToString(),
            AutoSize  = true,
            Font      = new Font("Yu Gothic UI", 9f, FontStyle.Bold),
            ForeColor = SystemColors.GrayText,
            Padding   = new Padding(0, 5, 0, 0),
        };
        zPanel.Controls.Add(_lblZValue);

        // --- 接続先グループ ---
        var connGroup = new GroupBox
        {
            Text = $"接続先 Room ID（未接続は {NumberFormatHelper.FormatByte(0xFF, format)}）",
            Dock = DockStyle.Fill,
            Font = new Font("Yu Gothic UI", 9f),
        };

        var connTable = new TableLayoutPanel
        {
            Dock        = DockStyle.Fill,
            ColumnCount = 4,
            RowCount    = 3,
            Padding     = new Padding(8, 4, 8, 4),
        };
        connTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 50));   // ラベル列（左）
        connTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));    // TextBox列（左）
        connTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 50));   // ラベル列（右）
        connTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));    // TextBox列（右）
        connTable.RowStyles.Add(new RowStyle(SizeType.Percent, 33));
        connTable.RowStyles.Add(new RowStyle(SizeType.Percent, 33));
        connTable.RowStyles.Add(new RowStyle(SizeType.Percent, 34));

        var h = page.Header;
        _txtLeft  = CreateRoomIdBox(h.LeftPage);
        _txtRight = CreateRoomIdBox(h.RightPage);
        _txtUp    = CreateRoomIdBox(h.UpPage);
        _txtDown  = CreateRoomIdBox(h.DownPage);
        _txtBack  = CreateRoomIdBox(h.BackPage);
        _txtFront = CreateRoomIdBox(h.FrontPage);

        AddLabelAndControl(connTable, "上:",   _txtUp,    0, 0);
        AddLabelAndControl(connTable, "下:",   _txtDown,  0, 2);
        AddLabelAndControl(connTable, "左:",   _txtLeft,  1, 0);
        AddLabelAndControl(connTable, "右:",   _txtRight, 1, 2);
        AddLabelAndControl(connTable, "奥:",   _txtBack,  2, 0);
        AddLabelAndControl(connTable, "手前:", _txtFront, 2, 2);

        connGroup.Controls.Add(connTable);

        // --- スクロール種別グループ ---
        var scrollGroup = new GroupBox
        {
            Text = "スクロール種別",
            Dock = DockStyle.Fill,
            Font = new Font("Yu Gothic UI", 9f),
        };

        var scrollTable = new TableLayoutPanel
        {
            Dock        = DockStyle.Fill,
            ColumnCount = 4,
            RowCount    = 2,
            Padding     = new Padding(8, 4, 8, 4),
        };
        scrollTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 50));
        scrollTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        scrollTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 50));
        scrollTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        scrollTable.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        scrollTable.RowStyles.Add(new RowStyle(SizeType.Percent, 50));

        _cmbScrollLeft  = CreateScrollCombo((ScrollType)h.ScrollLeft);
        _cmbScrollRight = CreateScrollCombo((ScrollType)h.ScrollRight);
        _cmbScrollUp    = CreateScrollCombo((ScrollType)h.ScrollUp);
        _cmbScrollDown  = CreateScrollCombo((ScrollType)h.ScrollDown);

        AddLabelAndControl(scrollTable, "上:", _cmbScrollUp,    0, 0);
        AddLabelAndControl(scrollTable, "下:", _cmbScrollDown,  0, 2);
        AddLabelAndControl(scrollTable, "左:", _cmbScrollLeft,  1, 0);
        AddLabelAndControl(scrollTable, "右:", _cmbScrollRight, 1, 2);

        scrollGroup.Controls.Add(scrollTable);

        // --- その他グループ ---
        var otherGroup = new GroupBox
        {
            Text = "その他",
            Dock = DockStyle.Fill,
            Font = new Font("Yu Gothic UI", 9f),
        };

        var otherTable = new TableLayoutPanel
        {
            Dock        = DockStyle.Fill,
            ColumnCount = 2,
            RowCount    = 4,
            Padding     = new Padding(8, 4, 8, 4),
        };
        otherTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80));
        otherTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        otherTable.RowStyles.Add(new RowStyle(SizeType.AutoSize));   // Enable
        otherTable.RowStyles.Add(new RowStyle(SizeType.AutoSize));   // Flags（6個・折り返しあり）
        otherTable.RowStyles.Add(new RowStyle(SizeType.AutoSize));   // Remarks
        otherTable.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // 余白吸収

        _chkEnable          = new CheckBox { Text = "有効",           Checked = page.Enable, Dock = DockStyle.Fill };
        _chkContinuePoint   = new CheckBox { Text = "Continue Point", Checked = h.Flags.HasFlag(PageFlags.ContinuePoint),   Dock = DockStyle.Fill };
        _chkNoScrollBack    = new CheckBox { Text = "No Scroll Back", Checked = h.Flags.HasFlag(PageFlags.NoScrollBack),    Dock = DockStyle.Fill };
        _chkPostEffects     = new CheckBox { Text = "Post Effects",   Checked = h.Flags.HasFlag(PageFlags.PostEffects),     Dock = DockStyle.Fill };
        _chkDarkness        = new CheckBox { Text = "Darkness",       Checked = h.Flags.HasFlag(PageFlags.Darkness),        Dock = DockStyle.Fill };
        _chkWind            = new CheckBox { Text = "Wind",           Checked = h.Flags.HasFlag(PageFlags.Wind),            Dock = DockStyle.Fill };
        _chkGravityModifier = new CheckBox { Text = "Gravity Mod.",   Checked = h.Flags.HasFlag(PageFlags.GravityModifier), Dock = DockStyle.Fill };
        _txtRemarks = new TextBox
        {
            Text      = page.Remarks,
            Dock      = DockStyle.Fill,
            Multiline = false,
        };

        otherTable.Controls.Add(new Label { Text = "Enable:", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, 0);
        otherTable.Controls.Add(_chkEnable, 1, 0);
        otherTable.Controls.Add(new Label { Text = "Flags:", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, 1);
        var flagsFlow = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, WrapContents = true, AutoSize = true };
        flagsFlow.Controls.AddRange([_chkContinuePoint, _chkNoScrollBack, _chkPostEffects, _chkDarkness, _chkWind, _chkGravityModifier]);
        otherTable.Controls.Add(flagsFlow, 1, 1);
        otherTable.Controls.Add(new Label { Text = "Remarks:", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, 2);
        otherTable.Controls.Add(_txtRemarks, 1, 2);

        otherGroup.Controls.Add(otherTable);

        // --- ボタン ---
        var buttonPanel = new FlowLayoutPanel
        {
            Dock          = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents  = false,
            Padding       = new Padding(0, 6, 0, 0),
        };

        var cancelButton = new Button { Text = "キャンセル", Width = 90, Height = 30, DialogResult = DialogResult.Cancel };
        var okButton     = new Button { Text = "OK",         Width = 90, Height = 30, DialogResult = DialogResult.None };
        okButton.Click += OnOkClick;

        buttonPanel.Controls.AddRange([cancelButton, okButton]);

        layout.Controls.Add(zPanel,      0, 0);
        layout.Controls.Add(connGroup,   0, 1);
        layout.Controls.Add(scrollGroup, 0, 2);
        layout.Controls.Add(otherGroup,  0, 3);
        layout.Controls.Add(buttonPanel, 0, 4);

        Controls.Add(layout);
        AcceptButton = okButton;
        CancelButton = cancelButton;
    }

    //========================
    // OKボタン
    //========================
    private void OnOkClick(object? sender, EventArgs e)
    {
        // 接続先バリデーション（変更なし）
        if (!TryParseRoomId(_txtLeft.Text,  out var left)  ||
            !TryParseRoomId(_txtRight.Text, out var right) ||
            !TryParseRoomId(_txtUp.Text,    out var up)    ||
            !TryParseRoomId(_txtDown.Text,  out var down)  ||
            !TryParseRoomId(_txtBack.Text,  out var back)  ||
            !TryParseRoomId(_txtFront.Text, out var front))
        {
            var hint = _format == NumberDisplayFormat.Hex ? "00〜FFの16進数" : "0〜255の数値";
            MessageBox.Show($"Room IDは{hint}で入力してください。",
                "入力エラー", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var flags = PageFlags.None;
        if (_chkContinuePoint.Checked)   flags |= PageFlags.ContinuePoint;
        if (_chkNoScrollBack.Checked)    flags |= PageFlags.NoScrollBack;
        if (_chkPostEffects.Checked)     flags |= PageFlags.PostEffects;
        if (_chkDarkness.Checked)        flags |= PageFlags.Darkness;
        if (_chkWind.Checked)            flags |= PageFlags.Wind;
        if (_chkGravityModifier.Checked) flags |= PageFlags.GravityModifier;

        // 元のHeaderをベースに、変更点だけ組み立てる（_page.Headerには触れない）
        var h = _page.Header;
        h.LeftPage  = left;
        h.RightPage = right;
        h.UpPage    = up;
        h.DownPage  = down;
        h.BackPage  = back;
        h.FrontPage = front;

        h.ScrollLeft  = (byte)GetScrollType(_cmbScrollLeft);
        h.ScrollRight = (byte)GetScrollType(_cmbScrollRight);
        h.ScrollUp    = (byte)GetScrollType(_cmbScrollUp);
        h.ScrollDown  = (byte)GetScrollType(_cmbScrollDown);

        h.Flags = flags;

        ResultHeader  = h;
        ResultEnable  = _chkEnable.Checked;
        ResultRemarks = _txtRemarks.Text;
        DialogResult = DialogResult.OK;
        Close();
    }

    //========================
    // ヘルパー
    //========================
    private bool TryParseRoomId(string text, out byte value)
    {
        if (NumberFormatHelper.TryParseByte(text, _format, out value)) return true;
        value = 0xFF;
        return false;
    }

    private static ScrollType GetScrollType(ComboBox combo)
        => combo.SelectedItem is ScrollType t ? t : ScrollType.None;

    private TextBox CreateRoomIdBox(byte value)
    {
        return new TextBox
        {
            Text   = NumberFormatHelper.FormatByte(value, _format),
            Anchor = AnchorStyles.Left | AnchorStyles.Right,
            Font   = new Font("Yu Gothic UI", 9f),
        };
    }

    private static ComboBox CreateScrollCombo(ScrollType selected)
    {
        var combo = new ComboBox
        {
            Anchor        = AnchorStyles.Left | AnchorStyles.Right,
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font          = new Font("Yu Gothic UI", 9f),
        };

        // DataSourceではなくItems.Addで追加する。
        // DataSourceはバインディング完了前にSelectedItemを設定できないため。
        foreach (var value in Enum.GetValues<ScrollType>())
            combo.Items.Add(value);

        combo.SelectedItem = selected;
        return combo;
    }

    private static void AddLabelAndControl(
        TableLayoutPanel table, string labelText, Control control, int row, int col)
    {
        var label = new Label
        {
            Text      = labelText,
            Dock      = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Font      = new Font("Yu Gothic UI", 9f),
        };
        table.Controls.Add(label, col, row);
        table.Controls.Add(control, col + 1, row);

        // コントロールをセル内で垂直中央に配置する
        table.SetCellPosition(control, new TableLayoutPanelCellPosition(col + 1, row));
        table.SetRowSpan(control, 1);
        control.Anchor = AnchorStyles.Left | AnchorStyles.Right;
    }
}
