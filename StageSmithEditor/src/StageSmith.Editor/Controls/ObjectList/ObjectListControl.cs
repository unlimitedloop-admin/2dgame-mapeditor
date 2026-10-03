using StageSmith.Application.Services;
using StageSmith.Core.Constants;
using StageSmith.Core.Models;
using StageSmith.Editor.Utilities;

namespace StageSmith.Editor.Controls;

/// <summary>
/// 配置オブジェクトの検索・一覧パネル。
/// 条件（キーワード・kind・palette・現在のページのみ）で絞り込んだ結果を表形式で表示する。
/// ページ移動や選択などの実際の操作は行わず、HitActivated で MainForm に委譲する。
/// </summary>
public sealed class ObjectListControl : UserControl
{
    private enum Column { Id, Kind, Palette, Page, Room, X, Y, Respawn }

    private readonly TextBox _keywordTextBox;
    private readonly ComboBox _kindCombo;
    private readonly ComboBox _paletteCombo;
    private readonly CheckBox _currentPageOnlyCheckBox;
    private readonly CheckBox _highlightCheckBox;
    private readonly Label _countLabel;
    private readonly ListView _listView;

    private Stage? _stage;
    private int _currentPageIndex;
    private bool _isUpdatingFilters;

    private Column _sortColumn = Column.Page;
    private bool _sortAscending = true;

    /// <summary>現在の検索結果（検索順）。</summary>
    public IReadOnlyList<EntitySearchHit> Hits { get; private set; } = [];

    /// <summary>
    /// マップ上で検索結果をハイライトするか。
    /// 絞り込み条件（キーワード・kind・palette）が無いときは全件が一致するだけなので、ハイライトしない。
    /// （「現在のページのみ」は範囲の指定なので条件に含めない）
    /// </summary>
    public bool ShowHighlight => _highlightCheckBox.Checked && HasActiveFilter;

    private bool HasActiveFilter =>
        !string.IsNullOrWhiteSpace(_keywordTextBox.Text) ||
        ChoiceItem.GetValue(_kindCombo) != null ||
        ChoiceItem.GetValue(_paletteCombo) != null;

    /// <summary>
    /// 一覧（ListView）自体にフォーカスがあるか。Delete / Ctrl+A は、絞り込み欄ではなく一覧を操作しているときだけ扱う。
    /// </summary>
    public bool IsListFocused => _listView.Focused;

    /// <summary>一覧の項目がダブルクリック・Enter・前へ／次へで選ばれたとき。</summary>
    public event Action<EntitySearchHit>? HitActivated;

    /// <summary>検索結果、またはハイライトの ON/OFF が変わったとき。</summary>
    public event Action? HitsChanged;

    /// <summary>
    /// 削除が要求されたとき。引数は削除対象と、「一覧の結果すべて」かどうか（false なら選択分）。
    /// 確認・Undo登録は MainForm が行う。
    /// </summary>
    public event Action<IReadOnlyList<EntitySearchHit>, bool>? DeleteRequested;

    public ObjectListControl()
    {
        Dock = DockStyle.Fill;

        _keywordTextBox = new TextBox { Dock = DockStyle.Fill, PlaceholderText = "ID・kind で検索" };
        _kindCombo = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
        _paletteCombo = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
        _currentPageOnlyCheckBox = new CheckBox { Text = "現在のページのみ", AutoSize = true };
        _highlightCheckBox = new CheckBox { Text = "ハイライト", AutoSize = true, Checked = true };
        _countLabel = new Label { AutoSize = true, Anchor = AnchorStyles.Left, Padding = new Padding(0, 6, 0, 0) };

        var prevButton = new Button { Text = "▲ 前へ", AutoSize = true };
        var nextButton = new Button { Text = "▼ 次へ", AutoSize = true };
        var deleteButton = new Button { Text = "選択を削除", AutoSize = true };
        var toolTip = new ToolTip();
        toolTip.SetToolTip(prevButton, "前の結果へ（オブジェクトツール使用中は Shift+F4）");
        toolTip.SetToolTip(nextButton, "次の結果へ（オブジェクトツール使用中は F4）");
        toolTip.SetToolTip(deleteButton, "一覧で選んだオブジェクトを削除（Delete）");
        Disposed += (_, _) => toolTip.Dispose();

        var filters = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 2, Padding = new Padding(3) };
        filters.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        filters.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        AddFilterRow(filters, "検索:", _keywordTextBox);
        AddFilterRow(filters, "Kind:", _kindCombo);
        AddFilterRow(filters, "Palette:", _paletteCombo);
        AddFilterRow(filters, "", Flow(_currentPageOnlyCheckBox, _highlightCheckBox));
        AddFilterRow(filters, "", Flow(_countLabel, prevButton, nextButton));
        AddFilterRow(filters, "", Flow(deleteButton));

        _listView = new ListView
        {
            Dock = DockStyle.Fill,
            View = View.Details,
            FullRowSelect = true,
            MultiSelect = true,   // 次の作業（一括削除）で複数選択を使う
            HideSelection = false,
            GridLines = true,
        };
        foreach (var (title, width) in new[] { ("ID", 90), ("Kind", 70), ("Palette", 60), ("Page", 80), ("Room", 44), ("X", 40), ("Y", 40), ("Respawn", 90) })
            _listView.Columns.Add(title, width);

        Controls.Add(_listView);
        Controls.Add(filters);

        _keywordTextBox.TextChanged += (_, _) => RunSearch();
        _kindCombo.SelectedIndexChanged += (_, _) => RunSearch();
        _paletteCombo.SelectedIndexChanged += (_, _) => RunSearch();
        _currentPageOnlyCheckBox.CheckedChanged += (_, _) => RunSearch();
        _highlightCheckBox.CheckedChanged += (_, _) => HitsChanged?.Invoke();

        prevButton.Click += (_, _) => SelectPrevious();
        nextButton.Click += (_, _) => SelectNext();
        deleteButton.Click += (_, _) => DeleteSelected();

        _listView.ItemActivate += (_, _) =>
        {
            if (_listView.FocusedItem?.Tag is EntitySearchHit hit) HitActivated?.Invoke(hit);
        };
        _listView.ColumnClick += (_, e) => SortBy((Column)e.Column);

        var menu = new ContextMenuStrip();
        menu.Items.Add(new ToolStripMenuItem("すべて選択", null, (_, _) => SelectAll()) { ShortcutKeyDisplayString = "Ctrl+A" });
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("選択を削除...", null, (_, _) => DeleteSelected());
        menu.Items.Add("一覧の結果をすべて削除...", null, (_, _) => DeleteAllResults());
        _listView.ContextMenuStrip = menu;
        Disposed += (_, _) => menu.Dispose();

        RebuildFilterCandidates();
        UpdateCount();
    }

    //========================
    // 公開API
    //========================

    /// <summary>
    /// 対象ステージ・現在のページを反映し、同じ条件で検索し直す（配置の変更・Undo・ページ切替のたび呼ぶ）。
    /// </summary>
    public void Reload(Stage? stage, int currentPageIndex)
    {
        _stage = stage;
        _currentPageIndex = currentPageIndex;

        RebuildFilterCandidates();
        RunSearch();
    }

    public void SelectNext() => MoveSelection(+1);

    public void SelectPrevious() => MoveSelection(-1);

    /// <summary>一覧で選択中のオブジェクトの削除を要求する（Delete キー・ボタン・右クリックから）。</summary>
    /// <summary>一覧の項目をすべて選択する（Ctrl+A・右クリックから）。</summary>
    public void SelectAll()
    {
        _listView.BeginUpdate();
        foreach (ListViewItem item in _listView.Items)
            item.Selected = true;
        _listView.EndUpdate();
    }

    public void DeleteSelected()
    {
        var selected = _listView.SelectedItems.Cast<ListViewItem>()
            .Select(i => (EntitySearchHit)i.Tag!)
            .ToList();

        DeleteRequested?.Invoke(selected, false);
    }

    private void DeleteAllResults() => DeleteRequested?.Invoke(Hits, true);

    //========================
    // 検索
    //========================

    private EntitySearchQuery BuildQuery() => new(
        Keyword: _keywordTextBox.Text,
        Kind: ChoiceItem.GetValue(_kindCombo),
        Palette: ChoiceItem.GetValue(_paletteCombo),
        PageIndex: _currentPageOnlyCheckBox.Checked ? _currentPageIndex : null);

    private void RunSearch()
    {
        if (_isUpdatingFilters) return;

        Hits = _stage == null ? [] : EntitySearchService.Search(_stage, BuildQuery());

        // 再検索しても、同じオブジェクトの選択は維持する
        var selectedIds = _listView.SelectedItems.Cast<ListViewItem>()
            .Select(i => ((EntitySearchHit)i.Tag!).Entity.Id)
            .ToHashSet();

        _listView.BeginUpdate();
        _listView.Items.Clear();
        foreach (var hit in SortHits(Hits))
        {
            var item = CreateItem(hit);
            item.Selected = selectedIds.Contains(hit.Entity.Id);
            _listView.Items.Add(item);
        }
        _listView.EndUpdate();

        UpdateCount();
        HitsChanged?.Invoke();
    }

    /// <summary>
    /// kind / palette の候補を、ステージに実際に配置されている値から作り直す（選択中の値は維持）。
    /// </summary>
    private void RebuildFilterCandidates()
    {
        var entities = _stage?.EnumerateEntities().Select(x => x.Entity).ToList() ?? [];

        _isUpdatingFilters = true;
        try
        {
            RebuildChoices(_kindCombo, "(すべて)",
                entities.Select(e => e.Properties.Kind).Where(k => k.Length > 0),
                extra: null);

            RebuildChoices(_paletteCombo, "(すべて)",
                entities.Select(e => e.Properties.Palette).OfType<string>(),
                extra: new ChoiceItem($"(既定: {EntityConstants.DefaultPalette})", EntitySearchQuery.DefaultPalette));
        }
        finally
        {
            _isUpdatingFilters = false;
        }
    }

    private static void RebuildChoices(ComboBox combo, string allLabel, IEnumerable<string> values, ChoiceItem? extra)
    {
        var current = ChoiceItem.GetValue(combo);

        combo.BeginUpdate();
        combo.Items.Clear();
        combo.Items.Add(new ChoiceItem(allLabel, null));
        if (extra != null) combo.Items.Add(extra);
        foreach (var value in values.Distinct().Order(StringComparer.Ordinal))
            combo.Items.Add(new ChoiceItem(value, value));
        combo.EndUpdate();

        // 選んでいた値が配置から消えていたら「すべて」に戻す
        ChoiceItem.Select(combo, current);
    }

    //========================
    // 表示
    //========================

    private static ListViewItem CreateItem(EntitySearchHit hit)
    {
        var e = hit.Entity;
        var p = e.Properties;

        var item = new ListViewItem(e.EntityId) { Tag = hit };
        item.SubItems.Add(p.Kind);
        item.SubItems.Add(p.Palette ?? "-");
        item.SubItems.Add(hit.Page.Name);
        item.SubItems.Add(hit.Page.Header.RoomId.ToString());
        item.SubItems.Add(e.X.ToString());
        item.SubItems.Add(e.Y.ToString());
        item.SubItems.Add(p.Respawn ?? "-");
        return item;
    }

    private void UpdateCount()
    {
        var total = _stage?.EnumerateEntities().Count() ?? 0;
        _countLabel.Text = $"{Hits.Count} / {total} 件";
    }

    private void SortBy(Column column)
    {
        _sortAscending = _sortColumn != column || !_sortAscending;
        _sortColumn = column;
        RunSearch();
    }

    private IEnumerable<EntitySearchHit> SortHits(IEnumerable<EntitySearchHit> hits)
    {
        // 同じ値の中では検索順（ページ → 配置順）を保つ（OrderBy は安定ソート）
        IComparable Key(EntitySearchHit h) => _sortColumn switch
        {
            Column.Id      => h.Entity.EntityId,
            Column.Kind    => h.Entity.Properties.Kind,
            Column.Palette => h.Entity.Properties.Palette ?? "",
            Column.Page    => h.PageIndex,
            Column.Room    => h.Page.Header.RoomId,
            Column.X       => h.Entity.X,
            Column.Y       => h.Entity.Y,
            Column.Respawn => h.Entity.Properties.Respawn ?? "",
            _              => h.PageIndex,
        };

        return _sortAscending ? hits.OrderBy(Key) : hits.OrderByDescending(Key);
    }

    private void MoveSelection(int delta)
    {
        var count = _listView.Items.Count;
        if (count == 0) return;

        var current = _listView.FocusedItem?.Index ?? (_listView.SelectedIndices.Count > 0 ? _listView.SelectedIndices[0] : -1);
        var next = current < 0
            ? (delta > 0 ? 0 : count - 1)
            : ((current + delta) % count + count) % count;   // 端では反対側へ回る（タイル検索の「繰り返し」と同じ）

        _listView.SelectedItems.Clear();
        var item = _listView.Items[next];
        item.Selected = true;
        item.Focused = true;
        item.EnsureVisible();

        HitActivated?.Invoke((EntitySearchHit)item.Tag!);
    }

    //========================
    // レイアウト補助
    //========================

    private static void AddFilterRow(TableLayoutPanel table, string label, Control control)
    {
        var row = table.RowCount++;
        table.Controls.Add(new Label { Text = label, AutoSize = true, Anchor = AnchorStyles.Left }, 0, row);
        table.Controls.Add(control, 1, row);
    }

    private static FlowLayoutPanel Flow(params Control[] controls)
    {
        var panel = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, WrapContents = false, Margin = Padding.Empty };
        panel.Controls.AddRange(controls);
        return panel;
    }
}
