using StageSmith.Core.Constants;
using StageSmith.Core.Models;

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
    private readonly CheckBox _chkWater;
    private readonly CheckBox _chkWind;
    private readonly TextBox  _txtRemarks;
    private readonly Label    _lblZValue;

    //========================
    // 初期化
    //========================
    public PageHeaderEditDialog(Page page)
    {
        _page        = page;
        ResultHeader = page.Header;

        Text            = $"ページヘッダー編集  -  Room ID: 0x{page.Header.RoomId:X2}";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition   = FormStartPosition.CenterParent;
        MaximizeBox     = false;
        MinimizeBox     = false;
        Size            = new Size(420, 500);

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
            Text = "接続先 Room ID（未接続は 255）",
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
        connTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 50));
        connTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 60));
        connTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 50));
        connTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
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

        _cmbScrollLeft  = CreateScrollCombo(ScrollEncoding.GetType(h.ScrollLeft));
        _cmbScrollRight = CreateScrollCombo(ScrollEncoding.GetType(h.ScrollRight));
        _cmbScrollUp    = CreateScrollCombo(ScrollEncoding.GetType(h.ScrollUp));
        _cmbScrollDown  = CreateScrollCombo(ScrollEncoding.GetType(h.ScrollDown));

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

        _chkEnable = new CheckBox { Text = "有効", Checked = page.Enable, Dock = DockStyle.Fill };
        _chkWater  = new CheckBox { Text = "Water", Checked = h.Flags.HasFlag(PageFlags.IsWater), Dock = DockStyle.Fill };
        _chkWind   = new CheckBox { Text = "Wind", Checked = h.Flags.HasFlag(PageFlags.IsWind), Dock = DockStyle.Fill };
        _txtRemarks = new TextBox
        {
            Text      = page.Remarks,
            Dock      = DockStyle.Fill,
            Multiline = false,
        };

        otherTable.Controls.Add(new Label { Text = "Enable:", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, 0);
        otherTable.Controls.Add(_chkEnable, 1, 0);
        otherTable.Controls.Add(new Label { Text = "Flags:", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, 1);
        var flagsFlow = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight };
        flagsFlow.Controls.AddRange([_chkWater, _chkWind]);
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
        // 接続先バリデーション
        if (!TryParseRoomId(_txtLeft.Text,  out var left)  ||
            !TryParseRoomId(_txtRight.Text, out var right) ||
            !TryParseRoomId(_txtUp.Text,    out var up)    ||
            !TryParseRoomId(_txtDown.Text,  out var down)  ||
            !TryParseRoomId(_txtBack.Text,  out var back)  ||
            !TryParseRoomId(_txtFront.Text, out var front))
        {
            MessageBox.Show("Room IDは0〜255の数値で入力してください。",
                "入力エラー", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        // フラグ
        var flags = PageFlags.None;
        if (_chkWater.Checked) flags |= PageFlags.IsWater;
        if (_chkWind.Checked)  flags |= PageFlags.IsWind;

        // PageHeader更新
        var h = _page.Header;
        h.LeftPage  = left;
        h.RightPage = right;
        h.UpPage    = up;
        h.DownPage  = down;
        h.BackPage  = back;
        h.FrontPage = front;

        h.ScrollLeft  = ScrollEncoding.Encode(GetScrollType(_cmbScrollLeft),  ScrollFlags.None);
        h.ScrollRight = ScrollEncoding.Encode(GetScrollType(_cmbScrollRight), ScrollFlags.None);
        h.ScrollUp    = ScrollEncoding.Encode(GetScrollType(_cmbScrollUp),    ScrollFlags.None);
        h.ScrollDown  = ScrollEncoding.Encode(GetScrollType(_cmbScrollDown),  ScrollFlags.None);

        h.Flags = flags;

        ResultHeader   = h;
        _page.Header   = h;
        _page.Enable   = _chkEnable.Checked;
        _page.Remarks  = _txtRemarks.Text;

        DialogResult = DialogResult.OK;
        Close();
    }

    //========================
    // ヘルパー
    //========================
    private static bool TryParseRoomId(string text, out byte value)
    {
        if (byte.TryParse(text, out value)) return true;
        value = 0xFF;
        return false;
    }

    private static ScrollType GetScrollType(ComboBox combo)
        => combo.SelectedItem is ScrollType t ? t : ScrollType.None;

    private static TextBox CreateRoomIdBox(byte value)
    {
        return new TextBox
        {
            Text  = value.ToString(),
            Anchor = AnchorStyles.Left,
            Dock  = DockStyle.None,
            Font  = new Font("Yu Gothic UI", 9f),
            Width = 50,
        };
    }

    private static ComboBox CreateScrollCombo(ScrollType selected)
    {
        var combo = new ComboBox
        {
            Anchor        = AnchorStyles.Left,
            Dock          = DockStyle.None,
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
        table.Controls.Add(new Label
        {
            Text      = labelText,
            Dock      = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Font      = new Font("Yu Gothic UI", 9f),
        }, col, row);
        table.Controls.Add(control, col + 1, row);
    }
}
