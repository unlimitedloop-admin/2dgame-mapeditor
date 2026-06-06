using StageSmith.Core.Constants;
using StageSmith.Core.Models;

namespace StageSmith.Editor.Controls;

public sealed class PropertyWindowControl : UserControl
{
    private EditorContext? _context;

    private bool _isRefreshing;

    /// <summary>
    /// プロパティ上のデータが変更されたとき発火する。
    /// ステージエクスプローラーの更新など外部への通知に使用する。
    /// </summary>
    public event Action? DataChanged;

    private readonly TableLayoutPanel _table = new();

    private TextBox _projectNameTextBox = null!;
    private TextBox _stageNameTextBox = null!;
    private TextBox _pageNameTextBox = null!;

    private CheckBox _pageEnableCheckBox = null!;
    private CheckBox _pageReadOnlyCheckBox = null!;

    private TextBox _pageRemarksTextBox = null!;

    // Page Header - Flags
    private CheckBox _flagWaterCheckBox = null!;
    private CheckBox _flagWindCheckBox = null!;

    // Page Header - Room
    private NumericUpDown _roomIdNumeric = null!;
    private NumericUpDown _leftPageNumeric = null!;
    private NumericUpDown _rightPageNumeric = null!;
    private NumericUpDown _upPageNumeric = null!;
    private NumericUpDown _downPageNumeric = null!;
    private NumericUpDown _frontPageNumeric = null!;
    private NumericUpDown _backPageNumeric = null!;
    private NumericUpDown _zNumeric = null!;

    // Page Header - Scroll
    private ComboBox _scrollLeftCombo = null!;
    private ComboBox _scrollRightCombo = null!;
    private ComboBox _scrollUpCombo = null!;
    private ComboBox _scrollDownCombo = null!;

    private CheckBox _scrollLeftNoEdgeCheck = null!;
    private CheckBox _scrollRightNoEdgeCheck = null!;
    private CheckBox _scrollUpNoEdgeCheck = null!;
    private CheckBox _scrollDownNoEdgeCheck = null!;

    private CheckBox _scrollLeftLoopCheck = null!;
    private CheckBox _scrollRightLoopCheck = null!;
    private CheckBox _scrollUpLoopCheck = null!;
    private CheckBox _scrollDownLoopCheck = null!;

    public PropertyWindowControl()
    {
        Dock = DockStyle.Right;
        Width = 280;

        InitializeLayout();
    }

    public void Bind(EditorContext context)
    {
        _context = context;
        _context.ContextChanged += RefreshProperties;

        RefreshProperties();
    }

    private void InitializeLayout()
    {
        _table.Dock = DockStyle.Fill;
        _table.ColumnCount = 2;
        _table.RowCount = 0;
        _table.AutoScroll = true;

        _table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));
        _table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        Controls.Add(_table);

        AddHeader("Project", Color.FromArgb(200, 195, 245));
        _projectNameTextBox = AddTextRow("Name");

        AddHeader("Stage", Color.FromArgb(200, 255, 200));
        _stageNameTextBox = AddTextRow("Name");

        AddHeader("Page", Color.FromArgb(255, 200, 0));
        _pageNameTextBox = AddTextRow("Name");
        _pageEnableCheckBox = AddCheckRow("Enable");
        _pageReadOnlyCheckBox = AddCheckRow("ReadOnly");
        _pageRemarksTextBox = AddTextRow("Remarks");

        AddHeader("Page Header", Color.FromArgb(255, 220, 120));

        _roomIdNumeric = AddByteRow("Room ID");

        _flagWaterCheckBox = AddCheckRow("Water");
        _flagWindCheckBox = AddCheckRow("Wind");

        _leftPageNumeric = AddByteRow("Left Page");
        _rightPageNumeric = AddByteRow("Right Page");
        _upPageNumeric = AddByteRow("Up Page");
        _downPageNumeric = AddByteRow("Down Page");
        _frontPageNumeric = AddByteRow("Front Page");
        _backPageNumeric = AddByteRow("Back Page");

        _scrollLeftCombo = AddScrollTypeRow("Left Scroll");
        _scrollLeftNoEdgeCheck = AddCheckRow("Left NoEdge");
        _scrollLeftLoopCheck = AddCheckRow("Left Loop");

        _scrollRightCombo = AddScrollTypeRow("Right Scroll");
        _scrollRightNoEdgeCheck = AddCheckRow("Right NoEdge");
        _scrollRightLoopCheck = AddCheckRow("Right Loop");

        _scrollUpCombo = AddScrollTypeRow("Up Scroll");
        _scrollUpNoEdgeCheck = AddCheckRow("Up NoEdge");
        _scrollUpLoopCheck = AddCheckRow("Up Loop");

        _scrollDownCombo = AddScrollTypeRow("Down Scroll");
        _scrollDownNoEdgeCheck = AddCheckRow("Down NoEdge");
        _scrollDownLoopCheck = AddCheckRow("Down Loop");

        _zNumeric = AddByteRow("Z");

        BindEvents();
    }

    private void BindEvents()
    {
        _projectNameTextBox.TextChanged += (_, _) =>
        {
            if (_context?.Project == null) return;
            _context.Project.Name = _projectNameTextBox.Text;
            DataChanged?.Invoke();
        };

        _stageNameTextBox.TextChanged += (_, _) =>
        {
            if (_context?.CurrentStage == null) return;
            _context.CurrentStage.Name = _stageNameTextBox.Text;
            DataChanged?.Invoke();
        };

        _pageNameTextBox.TextChanged += (_, _) =>
        {
            if (_context?.CurrentPage == null) return;
            _context.CurrentPage.Name = _pageNameTextBox.Text;
            DataChanged?.Invoke();
        };

        _pageEnableCheckBox.CheckedChanged += (_, _) =>
        {
            if (_context?.CurrentPage == null) return;
            _context.CurrentPage.Enable = _pageEnableCheckBox.Checked;
            DataChanged?.Invoke();
        };

        _pageReadOnlyCheckBox.CheckedChanged += (_, _) =>
        {
            if (_context?.CurrentPage == null) return;
            _context.CurrentPage.ReadOnly = _pageReadOnlyCheckBox.Checked;
            DataChanged?.Invoke();
        };

        _pageRemarksTextBox.TextChanged += (_, _) =>
        {
            if (_context?.CurrentPage == null) return;
            _context.CurrentPage.Remarks = _pageRemarksTextBox.Text;
        };

        _roomIdNumeric.ValueChanged += (_, _) => UpdatePageHeader();

        _flagWaterCheckBox.CheckedChanged += (_, _) => UpdatePageHeader();
        _flagWindCheckBox.CheckedChanged += (_, _) => UpdatePageHeader();

        _leftPageNumeric.ValueChanged += (_, _) => UpdatePageHeader();
        _rightPageNumeric.ValueChanged += (_, _) => UpdatePageHeader();
        _upPageNumeric.ValueChanged += (_, _) => UpdatePageHeader();
        _downPageNumeric.ValueChanged += (_, _) => UpdatePageHeader();
        _frontPageNumeric.ValueChanged += (_, _) => UpdatePageHeader();
        _backPageNumeric.ValueChanged += (_, _) => UpdatePageHeader();
        _zNumeric.ValueChanged += (_, _) => UpdatePageHeader();

        _scrollLeftCombo.SelectedIndexChanged += (_, _) => UpdatePageHeader();
        _scrollRightCombo.SelectedIndexChanged += (_, _) => UpdatePageHeader();
        _scrollUpCombo.SelectedIndexChanged += (_, _) => UpdatePageHeader();
        _scrollDownCombo.SelectedIndexChanged += (_, _) => UpdatePageHeader();

        _scrollLeftNoEdgeCheck.CheckedChanged += (_, _) => UpdatePageHeader();
        _scrollRightNoEdgeCheck.CheckedChanged += (_, _) => UpdatePageHeader();
        _scrollUpNoEdgeCheck.CheckedChanged += (_, _) => UpdatePageHeader();
        _scrollDownNoEdgeCheck.CheckedChanged += (_, _) => UpdatePageHeader();

        _scrollLeftLoopCheck.CheckedChanged += (_, _) => UpdatePageHeader();
        _scrollRightLoopCheck.CheckedChanged += (_, _) => UpdatePageHeader();
        _scrollUpLoopCheck.CheckedChanged += (_, _) => UpdatePageHeader();
        _scrollDownLoopCheck.CheckedChanged += (_, _) => UpdatePageHeader();
    }

    public void RefreshProperties()
    {
        if (_context == null)
            return;

        _isRefreshing = true;

        _projectNameTextBox.Text = _context.Project?.Name ?? "";
        _stageNameTextBox.Text = _context.CurrentStage?.Name ?? "";

        var page = _context.CurrentPage;

        _pageNameTextBox.Text = page?.Name ?? "";
        _pageEnableCheckBox.Checked = page?.Enable ?? false;
        _pageReadOnlyCheckBox.Checked = page?.ReadOnly ?? false;
        _pageRemarksTextBox.Text = page?.Remarks ?? "";

        if (page != null)
        {
            var header = page.Header;

            _roomIdNumeric.Value = header.RoomId;

            _flagWaterCheckBox.Checked = header.Flags.HasFlag(PageFlags.IsWater);
            _flagWindCheckBox.Checked = header.Flags.HasFlag(PageFlags.IsWind);

            _leftPageNumeric.Value = header.LeftPage;
            _rightPageNumeric.Value = header.RightPage;
            _upPageNumeric.Value = header.UpPage;
            _downPageNumeric.Value = header.DownPage;
            _frontPageNumeric.Value = header.FrontPage;
            _backPageNumeric.Value = header.BackPage;
            _zNumeric.Value = header.Z;

            SetScrollControls(header.ScrollLeft, _scrollLeftCombo, _scrollLeftNoEdgeCheck, _scrollLeftLoopCheck);
            SetScrollControls(header.ScrollRight, _scrollRightCombo, _scrollRightNoEdgeCheck, _scrollRightLoopCheck);
            SetScrollControls(header.ScrollUp, _scrollUpCombo, _scrollUpNoEdgeCheck, _scrollUpLoopCheck);
            SetScrollControls(header.ScrollDown, _scrollDownCombo, _scrollDownNoEdgeCheck, _scrollDownLoopCheck);
        }

        _isRefreshing = false;
    }

    private void UpdatePageHeader()
    {
        if (_isRefreshing)
            return;

        var page = _context?.CurrentPage;
        if (page == null)
            return;

        var flags = PageFlags.None;

        if (_flagWaterCheckBox.Checked) flags |= PageFlags.IsWater;
        if (_flagWindCheckBox.Checked) flags |= PageFlags.IsWind;

        page.Header = new PageHeader
        {
            MagicStart = 0xA5,
            MagicEnd = 0x5A,

            RoomId = (byte)_roomIdNumeric.Value,

            Flags = flags,

            LeftPage = (byte)_leftPageNumeric.Value,
            RightPage = (byte)_rightPageNumeric.Value,
            UpPage = (byte)_upPageNumeric.Value,
            DownPage = (byte)_downPageNumeric.Value,
            FrontPage = (byte)_frontPageNumeric.Value,
            BackPage = (byte)_backPageNumeric.Value,

            ScrollLeft = GetScrollByte(_scrollLeftCombo, _scrollLeftNoEdgeCheck, _scrollLeftLoopCheck),
            ScrollRight = GetScrollByte(_scrollRightCombo, _scrollRightNoEdgeCheck, _scrollRightLoopCheck),
            ScrollUp = GetScrollByte(_scrollUpCombo, _scrollUpNoEdgeCheck, _scrollUpLoopCheck),
            ScrollDown = GetScrollByte(_scrollDownCombo, _scrollDownNoEdgeCheck, _scrollDownLoopCheck),

            Z = (byte)_zNumeric.Value,
        };
    }

    private void AddHeader(string text, Color color)
    {
        var label = new Label
        {
            Text = text,
            BackColor = color,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font(Font, FontStyle.Bold),
            Padding = new Padding(4)
        };

        AddRow(label, columnSpan: 2);
    }

    private TextBox AddTextRow(string labelText)
    {
        var textBox = new TextBox
        {
            Dock = DockStyle.Fill
        };

        textBox.KeyDown += (sender, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                SelectNextControl((Control)sender!, true, true, true, true);
            }
        };

        AddRow(CreateLabel(labelText), textBox);
        return textBox;
    }

    private CheckBox AddCheckRow(string labelText)
    {
        var checkBox = new CheckBox
        {
            Dock = DockStyle.Left
        };

        AddRow(CreateLabel(labelText), checkBox);
        return checkBox;
    }

    private static Label CreateLabel(string text)
    {
        return new Label
        {
            Text = text,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(4)
        };
    }

    private void AddRow(Control control, int columnSpan)
    {
        var row = _table.RowCount++;
        _table.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        _table.Controls.Add(control, 0, row);
        _table.SetColumnSpan(control, columnSpan);
    }

    private void AddRow(Control label, Control editor)
    {
        var row = _table.RowCount++;
        _table.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        _table.Controls.Add(label, 0, row);
        _table.Controls.Add(editor, 1, row);
    }

    private NumericUpDown AddByteRow(string labelText)
    {
        var numeric = new NumericUpDown
        {
            Dock = DockStyle.Fill,
            Minimum = 0,
            Maximum = 255,
            Value = 0
        };

        AddRow(CreateLabel(labelText), numeric);
        return numeric;
    }

    private ComboBox AddScrollTypeRow(string labelText)
    {
        var combo = new ComboBox
        {
            Dock = DockStyle.Fill,
            DropDownStyle = ComboBoxStyle.DropDownList,
            DataSource = Enum.GetValues<ScrollType>()
        };

        AddRow(CreateLabel(labelText), combo);
        return combo;
    }

    private static void SetScrollControls(
        byte value,
        ComboBox combo,
        CheckBox noEdgeCheck,
        CheckBox loopCheck)
    {
        var type = ScrollEncoding.GetType(value);
        var flags = ScrollEncoding.GetFlags(value);

        combo.SelectedItem = type;
        noEdgeCheck.Checked = flags.HasFlag(ScrollFlags.NoEdge);
        loopCheck.Checked = flags.HasFlag(ScrollFlags.Loop);
    }

    private static byte GetScrollByte(
        ComboBox combo,
        CheckBox noEdgeCheck,
        CheckBox loopCheck)
    {
        var type = combo.SelectedItem is ScrollType scrollType
            ? scrollType
            : ScrollType.None;

        var flags = ScrollFlags.None;

        if (noEdgeCheck.Checked) flags |= ScrollFlags.NoEdge;
        if (loopCheck.Checked) flags |= ScrollFlags.Loop;

        return ScrollEncoding.Encode(type, flags);
    }
}
