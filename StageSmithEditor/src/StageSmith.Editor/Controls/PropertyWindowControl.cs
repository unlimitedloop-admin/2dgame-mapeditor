using StageSmith.Application.Commands;
using StageSmith.Core.Constants;
using StageSmith.Core.Models;
using StageSmith.Editor.Forms;
using StageSmith.Editor.Utilities;

namespace StageSmith.Editor.Controls;

public sealed class PropertyWindowControl : UserControl
{
    private EditorContext? _context;

    private NumberDisplayFormat _numberDisplayFormat = NumberDisplayFormat.Hex;

    private bool _isRefreshing;

    /// <summary>
    /// プロパティ上のデータが変更されたとき発火する。
    /// ステージエクスプローラーの更新など外部への通知に使用する。
    /// </summary>
    public event Action? DataChanged;

    /// <summary>
    /// プロパティ編集がコマンドとして確定したとき発火する。
    /// 実際のCommandManager.Execute()はMainForm側で行う。
    /// </summary>
    public event Action<ICommand>? CommandRequested;

    private readonly Panel _scrollPanel = new();
    private readonly TableLayoutPanel _table = new();

    private TextBox _projectNameTextBox = null!;
    private TextBox _stageNameTextBox = null!;
    private TextBox _pageNameTextBox = null!;

    private FlowLayoutPanel _stageTagsBadgesPanel = null!;
    private FlowLayoutPanel _pageTagsBadgesPanel = null!;

    private const int TagIconSize = 16;

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

    public void SetNumberDisplayFormat(NumberDisplayFormat format)
    {
        _numberDisplayFormat = format;

        var hex = format == NumberDisplayFormat.Hex;

        _roomIdNumeric.Hexadecimal = hex;
        _leftPageNumeric.Hexadecimal = hex;
        _rightPageNumeric.Hexadecimal = hex;
        _upPageNumeric.Hexadecimal = hex;
        _downPageNumeric.Hexadecimal = hex;
        _frontPageNumeric.Hexadecimal = hex;
        _backPageNumeric.Hexadecimal = hex;
        _zNumeric.Hexadecimal = hex;

        RefreshProperties();
    }

    private void InitializeLayout()
    {
        _scrollPanel.Dock = DockStyle.Fill;
        _scrollPanel.AutoScroll = true;

        // AutoSize + Dock=Top にすることで、パネルの実際の高さが
        // コンテンツ量に合わせて計算されるようになり、外側の _scrollPanel の
        // AutoScroll が「収まりきらない」ことを正しく検知できるようになる。
        _table.Dock = DockStyle.Top;
        _table.AutoSize = true;
        _table.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        _table.ColumnCount = 2;
        _table.RowCount = 0;

        _table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));
        _table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        _scrollPanel.Controls.Add(_table);
        Controls.Add(_scrollPanel);

        AddHeader("Project", Color.FromArgb(200, 195, 245));
        _projectNameTextBox = AddTextRow("Name");

        AddHeader("Stage", Color.FromArgb(200, 255, 200));
        _stageNameTextBox = AddTextRow("Name");
        _stageTagsBadgesPanel = AddTagsRow("Tags", OnEditStageTagsClick);

        AddHeader("Page", Color.FromArgb(255, 200, 0));
        _pageNameTextBox = AddTextRow("Name");
        _pageEnableCheckBox = AddCheckRow("Enable");
        _pageReadOnlyCheckBox = AddCheckRow("ReadOnly");
        _pageRemarksTextBox = AddTextRow("Remarks");
        _pageTagsBadgesPanel = AddTagsRow("Tags", OnEditPageTagsClick);

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
        _scrollRightCombo = AddScrollTypeRow("Right Scroll");
        _scrollUpCombo = AddScrollTypeRow("Up Scroll");
        _scrollDownCombo = AddScrollTypeRow("Down Scroll");

        _zNumeric = AddByteRow("Z");

        BindEvents();
    }

    private void BindEvents()
    {
        TrackChange(_projectNameTextBox,
            () => _projectNameTextBox.Text,
            v => { if (_context?.Project != null) _context.Project.Name = v; });

        TrackChange(_stageNameTextBox,
            () => _stageNameTextBox.Text,
            v => { if (_context?.CurrentStage != null) _context.CurrentStage.Name = v; });

        TrackChange(_pageNameTextBox,
            () => _pageNameTextBox.Text,
            v => { if (_context?.CurrentPage != null) _context.CurrentPage.Name = v; });

        TrackChange(_pageEnableCheckBox,
            () => _pageEnableCheckBox.Checked,
            v => { if (_context?.CurrentPage != null) _context.CurrentPage.Enable = v; });

        TrackChange(_pageReadOnlyCheckBox,
            () => _pageReadOnlyCheckBox.Checked,
            v => { if (_context?.CurrentPage != null) _context.CurrentPage.ReadOnly = v; });

        TrackChange(_pageRemarksTextBox,
            () => _pageRemarksTextBox.Text,
            v => { if (_context?.CurrentPage != null) _context.CurrentPage.Remarks = v; });

        foreach (var control in new Control[]
        {
            _roomIdNumeric, _flagWaterCheckBox, _flagWindCheckBox,
            _leftPageNumeric, _rightPageNumeric, _upPageNumeric, _downPageNumeric,
            _frontPageNumeric, _backPageNumeric, _zNumeric,
            _scrollLeftCombo, _scrollRightCombo, _scrollUpCombo, _scrollDownCombo,
        })
        {
            control.Leave += (_, _) => UpdatePageHeader();
        }
    }

    public void RefreshProperties()
    {
        if (_context == null)
            return;

        _isRefreshing = true;

        _projectNameTextBox.Text = _context.Project?.Name ?? "";
        _stageNameTextBox.Text = _context.CurrentStage?.Name ?? "";
        RebuildTagBadges(_stageTagsBadgesPanel, _context.Project, _context.CurrentStage?.TagIds);

        var page = _context.CurrentPage;

        _pageNameTextBox.Text = page?.Name ?? "";
        _pageEnableCheckBox.Checked = page?.Enable ?? false;
        _pageReadOnlyCheckBox.Checked = page?.ReadOnly ?? false;
        _pageRemarksTextBox.Text = page?.Remarks ?? "";
        RebuildTagBadges(_pageTagsBadgesPanel, _context.Project, page?.TagIds);

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

            _scrollLeftCombo.SelectedItem = (ScrollType)header.ScrollLeft;
            _scrollRightCombo.SelectedItem = (ScrollType)header.ScrollRight;
            _scrollUpCombo.SelectedItem = (ScrollType)header.ScrollUp;
            _scrollDownCombo.SelectedItem = (ScrollType)header.ScrollDown;
        }

        _isRefreshing = false;
    }

    private void UpdatePageHeader()
    {
        if (_isRefreshing) return;

        var page = _context?.CurrentPage;
        if (page == null) return;

        var oldHeader = page.Header;
        var newHeader = BuildHeaderFromControls();

        if (oldHeader.ToBytes().SequenceEqual(newHeader.ToBytes())) return;

        CommandRequested?.Invoke(new ActionCommand(
            () => { page.Header = newHeader; DataChanged?.Invoke(); },
            () => { page.Header = oldHeader; DataChanged?.Invoke(); }
        ));
    }

    /// <summary>
    /// バッジ表示を再構築する。優先度昇順（Priority値が小さいほど先頭）で並べる。
    /// 画像リソースはバッジ（PictureBox）のDisposeに追従して解放される。
    /// </summary>
    private static void RebuildTagBadges(FlowLayoutPanel container, EditorProject? project, List<string>? tagIds)
    {
        foreach (Control old in container.Controls)
            old.Dispose();
        container.Controls.Clear();

        var tags = (project == null || tagIds == null)
            ? Enumerable.Empty<Tag>()
            : tagIds
                .Select(idStr => Guid.TryParse(idStr, out var id) ? project.FindTag(id) : null)
                .Where(t => t != null)
                .Select(t => t!)
                .OrderBy(t => t.Priority)
                .ThenBy(t => t.Label);

        var tagList = tags.ToList();

        if (tagList.Count == 0)
        {
            container.Controls.Add(new Label { Text = "-", AutoSize = true, Padding = new Padding(0, 3, 0, 0) });
            return;
        }

        foreach (var tag in tagList)
            container.Controls.Add(CreateTagBadge(tag, project!.BaseDirectory));
    }

    private static Panel CreateTagBadge(Tag tag, string baseDirectory)
    {
        var badgeColor = ColorTranslator.FromHtml(tag.Color);

        var badge = new Panel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            BackColor = badgeColor,
            Padding = new Padding(4, 2, 4, 2),
            Margin = new Padding(0, 0, 4, 4),
            BorderStyle = BorderStyle.FixedSingle,
        };

        var inner = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.LeftToRight,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            WrapContents = false,
        };

        var icon = TagIconRenderer.SafeLoad(
            string.IsNullOrWhiteSpace(tag.IconPath) ? null : Path.Combine(baseDirectory, tag.IconPath));

        if (icon != null)
        {
            inner.Controls.Add(new PictureBox
            {
                Image = icon,
                Size = new Size(TagIconSize, TagIconSize),
                SizeMode = PictureBoxSizeMode.Zoom,
                Margin = new Padding(0, 0, 3, 0),
            });
        }

        inner.Controls.Add(new Label
        {
            Text = tag.Label,
            AutoSize = true,
            ForeColor = IsLightColor(badgeColor) ? Color.Black : Color.White,
            Margin = new Padding(0),
        });

        badge.Controls.Add(inner);
        return badge;
    }

    private static bool IsLightColor(Color color)
        => (color.R * 0.299 + color.G * 0.587 + color.B * 0.114) > 128;

    private void OnEditStageTagsClick(object? sender, EventArgs e)
    {
        var stage = _context?.CurrentStage;
        var project = _context?.Project;
        if (stage == null || project == null) return;

        var oldIds = stage.TagIds.ToList();
        var oldGuids = oldIds.Where(s => Guid.TryParse(s, out _)).Select(Guid.Parse).ToList();

        using var dialog = new TagAssignDialog(project.Tags, oldGuids);
        if (dialog.ShowDialog(FindForm()) != DialogResult.OK) return;

        var newIds = dialog.SelectedTagIds.Select(id => id.ToString()).ToList();
        if (oldIds.SequenceEqual(newIds)) return;

        CommandRequested?.Invoke(new ActionCommand(
            () => { stage.TagIds = newIds; stage.MarkDirty(); DataChanged?.Invoke(); RefreshProperties(); },
            () => { stage.TagIds = oldIds; stage.MarkDirty(); DataChanged?.Invoke(); RefreshProperties(); }
        ));
    }

    private void OnEditPageTagsClick(object? sender, EventArgs e)
    {
        var page = _context?.CurrentPage;
        var project = _context?.Project;
        if (page == null || project == null) return;

        var oldIds = page.TagIds.ToList();
        var oldGuids = oldIds.Where(s => Guid.TryParse(s, out _)).Select(Guid.Parse).ToList();

        using var dialog = new TagAssignDialog(project.Tags, oldGuids);
        if (dialog.ShowDialog(FindForm()) != DialogResult.OK) return;

        var newIds = dialog.SelectedTagIds.Select(id => id.ToString()).ToList();
        if (oldIds.SequenceEqual(newIds)) return;

        var stage = _context?.CurrentStage;

        CommandRequested?.Invoke(new ActionCommand(
            () => { page.TagIds = newIds; stage?.MarkDirty(); DataChanged?.Invoke(); RefreshProperties(); },
            () => { page.TagIds = oldIds; stage?.MarkDirty(); DataChanged?.Invoke(); RefreshProperties(); }
        ));
    }

    private PageHeader BuildHeaderFromControls()
    {
        var flags = PageFlags.None;
        if (_flagWaterCheckBox.Checked) flags |= PageFlags.IsWater;
        if (_flagWindCheckBox.Checked) flags |= PageFlags.IsWind;

        return new PageHeader
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
            ScrollLeft = GetScrollByte(_scrollLeftCombo),
            ScrollRight = GetScrollByte(_scrollRightCombo),
            ScrollUp = GetScrollByte(_scrollUpCombo),
            ScrollDown = GetScrollByte(_scrollDownCombo),
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

    private FlowLayoutPanel AddTagsRow(string labelText, EventHandler onEditClick)
    {
        var row = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            AutoSize = true,
        };

        var badgesPanel = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            AutoSize = true,
            Margin = new Padding(0, 2, 6, 0),
        };

        var editButton = new Button
        {
            Text = "編集...",
            AutoSize = true,
        };
        editButton.Click += onEditClick;

        row.Controls.Add(badgesPanel);
        row.Controls.Add(editButton);

        AddRow(CreateLabel(labelText), row);
        return badgesPanel;
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

    private static byte GetScrollByte(ComboBox combo)
    {
        var type = combo.SelectedItem is ScrollType scrollType
            ? scrollType
            : ScrollType.None;

        return (byte)type;
    }

    /// <summary>
    /// フォーカスイン時点の値を記録し、フォーカスアウト時に変化があれば
    /// 1操作としてCommandRequestedを発火する汎用ヘルパー。
    /// </summary>
    private void TrackChange<TValue>(Control control, Func<TValue> getValue, Action<TValue> applyValue)
    {
        var before = getValue();

        control.Enter += (_, _) =>
        {
            if (_isRefreshing) return;
            before = getValue();
        };

        control.Leave += (_, _) =>
        {
            if (_isRefreshing) return;

            var after = getValue();
            if (EqualityComparer<TValue>.Default.Equals(before, after)) return;

            var capturedBefore = before;
            CommandRequested?.Invoke(new ActionCommand(
                () => { applyValue(after); DataChanged?.Invoke(); },
                () => { applyValue(capturedBefore); DataChanged?.Invoke(); }
            ));
        };
    }
}
