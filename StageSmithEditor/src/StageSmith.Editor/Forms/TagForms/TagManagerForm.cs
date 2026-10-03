using StageSmith.Core.Models;

namespace StageSmith.Editor.Forms;

/// <summary>
/// プロジェクトのタグを一覧表示し、追加・編集・削除するためのフォーム。
/// タグの追加・編集は、作業コピーを使って行い、Saveボタンで確定する。
/// </summary>
public sealed class TagManagerForm : Form
{
    private readonly EditorProject _project;

    private Tag _currentTag = new();
    private Guid? _editingTagId;
    private string? _pendingIconSourcePath; // Save時にコピーする、まだTagIcons/未反映のアイコン元パス
    private bool _pendingIconCleared;
    private bool _isReadOnly;

    private readonly ListBox _tagListBox = new();
    private readonly TextBox _labelTextBox = new();
    private readonly Panel _colorSwatch = new();
    private readonly Button _colorButton = new();
    private readonly PictureBox _iconPreview = new();
    private readonly Button _selectIconButton = new();
    private readonly Button _clearIconButton = new();
    private readonly NumericUpDown _priorityNumeric = new();
    private readonly Label _statusLabel = new();

    /// <summary>
    /// タグが変更されたときに発生するイベント。
    /// </summary>
    public event Action? TagsChanged;

    public TagManagerForm(EditorProject project)
    {
        _project = project ?? throw new ArgumentNullException(nameof(project));

        Text = "Tag Manager";
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(596, 307);    // デフォルトのサイズ。ユーザーがリサイズ可能

        InitializeLayout();

        RefreshTagList();
        SetWorkingTag(new Tag(), editingTagId: null);
    }

    private void InitializeLayout()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 2,
            Padding = new Padding(8),
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 200));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));

        // --- 左：一覧 ---
        _tagListBox.Dock = DockStyle.Fill;
        _tagListBox.DrawMode = DrawMode.OwnerDrawFixed;
        _tagListBox.ItemHeight = 28;
        _tagListBox.DrawItem += OnDrawTagListItem;
        _tagListBox.SelectedIndexChanged += (_, _) =>
        {
            if (_tagListBox.SelectedItem is TagListItem item)
                LoadSavedTag(item.Id);
        };

        // --- 右：編集パネル ---
        var editPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 5,
            Padding = new Padding(8, 0, 0, 0),
        };
        editPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 70));
        editPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        editPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));   // Label
        editPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));   // Color
        editPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));   // Icon
        editPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));   // Priority
        editPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        _labelTextBox.Dock = DockStyle.Fill;
        _labelTextBox.TextChanged += (_, _) => _currentTag.Label = _labelTextBox.Text;

        var colorRow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            AutoSize = true,
            Padding = new Padding(0, 4, 0, 4)
        };
        _colorSwatch.Size = new Size(24, 24);
        _colorSwatch.BorderStyle = BorderStyle.FixedSingle;
        _colorSwatch.BackColor = ColorTranslator.FromHtml(_currentTag.Color);
        _colorButton.Text = "変更...";
        _colorButton.AutoSize = true;
        _colorButton.Click += OnChangeColorClick;
        colorRow.Controls.Add(_colorSwatch);
        colorRow.Controls.Add(_colorButton);

        var iconRow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            AutoSize = true,
            Padding = new Padding(0, 4, 0, 4)
        };
        _iconPreview.Size = new Size(32, 32);
        _iconPreview.BorderStyle = BorderStyle.FixedSingle;
        _iconPreview.SizeMode = PictureBoxSizeMode.Zoom;
        _selectIconButton.Text = "選択...";
        _selectIconButton.AutoSize = true;
        _selectIconButton.Click += OnSelectIconClick;
        _clearIconButton.Text = "クリア";
        _clearIconButton.AutoSize = true;
        _clearIconButton.Click += (_, _) =>
        {
            _pendingIconSourcePath = null;
            _pendingIconCleared = true;
            _iconPreview.Image = null;
        };
        iconRow.Controls.Add(_iconPreview);
        iconRow.Controls.Add(_selectIconButton);
        iconRow.Controls.Add(_clearIconButton);

        _priorityNumeric.Minimum = 0;
        _priorityNumeric.Maximum = 999;
        _priorityNumeric.Dock = DockStyle.Left;
        _priorityNumeric.Width = 80;
        _priorityNumeric.ValueChanged += (_, _) => _currentTag.Priority = (int)_priorityNumeric.Value;

        editPanel.Controls.Add(new Label { Text = "Label:", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, 0);
        editPanel.Controls.Add(_labelTextBox, 1, 0);
        editPanel.Controls.Add(new Label { Text = "Color:", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, 1);
        editPanel.Controls.Add(colorRow, 1, 1);
        editPanel.Controls.Add(new Label { Text = "Icon:", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, 2);
        editPanel.Controls.Add(iconRow, 1, 2);
        editPanel.Controls.Add(new Label { Text = "Priority:", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, 3);
        editPanel.Controls.Add(_priorityNumeric, 1, 3);

        // --- 下：ボタン ---
        var buttonPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            Padding = new Padding(0, 6, 0, 6)
        };

        var newButton = new Button { Text = "New", AutoSize = true };
        newButton.Click += (_, _) => SetWorkingTag(new Tag(), editingTagId: null);

        var saveButton = new Button { Text = "Save", AutoSize = true };
        saveButton.Click += (_, _) => SaveCurrentTag();

        var deleteButton = new Button { Text = "Delete", AutoSize = true };
        deleteButton.Click += (_, _) => DeleteCurrentTag();

        _statusLabel.AutoSize = true;
        _statusLabel.Padding = new Padding(12, 6, 0, 0);

        buttonPanel.Controls.Add(newButton);
        buttonPanel.Controls.Add(saveButton);
        buttonPanel.Controls.Add(deleteButton);
        buttonPanel.Controls.Add(_statusLabel);

        root.Controls.Add(_tagListBox, 0, 0);
        root.SetRowSpan(_tagListBox, 2);
        root.Controls.Add(editPanel, 1, 0);
        root.Controls.Add(buttonPanel, 1, 1);

        Controls.Add(root);
    }

    /// <summary>
    /// タグリストの項目を描画する際に発生するイベント。
    /// </summary>
    private void OnDrawTagListItem(object? sender, DrawItemEventArgs e)
    {
        e.DrawBackground();

        if (e.Index >= 0 && _tagListBox.Items[e.Index] is TagListItem item)
        {
            var swatchRect = new Rectangle(e.Bounds.X + 4, e.Bounds.Y + 4, 20, 20);
            using (var brush = new SolidBrush(item.Color))
                e.Graphics.FillRectangle(brush, swatchRect);
            e.Graphics.DrawRectangle(Pens.Gray, swatchRect);

            var textRect = new Rectangle(e.Bounds.X + 30, e.Bounds.Y, e.Bounds.Width - 30, e.Bounds.Height);
            using var textBrush = new SolidBrush(e.ForeColor);
            e.Graphics.DrawString(item.Label, e.Font ?? SystemFonts.DefaultFont, textBrush, textRect,
                new StringFormat { LineAlignment = StringAlignment.Center });
        }

        e.DrawFocusRectangle();
    }

    /// <summary>
    /// カラー変更ボタンがクリックされたときに発生するイベント。
    /// </summary>
    private void OnChangeColorClick(object? sender, EventArgs e)
    {
        using var dialog = new ColorDialog { Color = ColorTranslator.FromHtml(_currentTag.Color) };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        _currentTag.Color = ColorTranslator.ToHtml(dialog.Color);
        _colorSwatch.BackColor = dialog.Color;
    }

    /// <summary>
    /// アイコン選択ボタンがクリックされたときに発生するイベント。
    /// </summary>
    private void OnSelectIconClick(object? sender, EventArgs e)
    {
        using var dialog = new TagIconPickerDialog();
        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        _pendingIconSourcePath = dialog.SelectedSourcePath;
        _pendingIconCleared = dialog.SelectedSourcePath == null;

        _iconPreview.Image = null;
        if (dialog.SelectedSourcePath != null && File.Exists(dialog.SelectedSourcePath))
        {
            try { _iconPreview.Image = new Bitmap(dialog.SelectedSourcePath); }
            catch { /* プレビュー失敗は無視。Save時に改めて検証される */ }
        }
    }

    /// <summary>
    /// 作業中のタグを設定する。
    /// </summary>
    /// <param name="source">設定するタグのソース。</param>
    /// <param name="editingTagId">編集中のタグのID。</param>
    private void SetWorkingTag(Tag source, Guid? editingTagId)
    {
        _currentTag = new Tag
        {
            Label = source.Label,
            Color = source.Color,
            Priority = source.Priority,
            IconPath = source.IconPath,
        };
        _editingTagId = editingTagId;
        _pendingIconSourcePath = null;
        _pendingIconCleared = false;

        _labelTextBox.Text = _currentTag.Label;
        _colorSwatch.BackColor = ColorTranslator.FromHtml(_currentTag.Color);
        _priorityNumeric.Value = _currentTag.Priority;

        _iconPreview.Image = null;
        if (!string.IsNullOrWhiteSpace(_currentTag.IconPath))
        {
            var fullPath = Path.Combine(_project.BaseDirectory, _currentTag.IconPath);
            if (File.Exists(fullPath))
            {
                try { _iconPreview.Image = new Bitmap(fullPath); }
                catch { /* 破損画像は無視 */ }
            }
        }

        UpdateStatus();
    }

    private void SaveCurrentTag()
    {
        if (_isReadOnly) return;

        _currentTag.Label = _labelTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(_currentTag.Label))
            _currentTag.Label = "New Tag";

        // アイコンの確定反映（選択済みならTagIcons/へコピー、クリア指定ならnull化）
        if (_pendingIconCleared)
        {
            _currentTag.IconPath = null;
        }
        else if (_pendingIconSourcePath != null)
        {
            _currentTag.IconPath = CopyIconIntoProject(_pendingIconSourcePath);
        }

        if (_editingTagId.HasValue)
        {
            var target = new Tag
            {
                Id = _editingTagId.Value,
                Label = _currentTag.Label,
                Color = _currentTag.Color,
                Priority = _currentTag.Priority,
                IconPath = _currentTag.IconPath,
            };

            // 値が何も変わっていなければ、未保存の変更として通知しない
            if (_project.FindTag(target.Id) is { } existing && IsSameTag(existing, target))
            {
                UpdateStatus("No changes.");
                return;
            }

            if (_project.UpdateTag(target))
            {
                RefreshTagList(selectId: _editingTagId);
                TagsChanged?.Invoke();
                UpdateStatus("Saved.");
                return;
            }
        }

        var saved = _project.AddTag(_currentTag);
        SetWorkingTag(saved, saved.Id);
        RefreshTagList(selectId: saved.Id);
        TagsChanged?.Invoke();
        UpdateStatus("Saved as new.");
    }

    private static bool IsSameTag(Tag a, Tag b)
        => a.Label == b.Label
        && a.Color == b.Color
        && a.Priority == b.Priority
        && a.IconPath == b.IconPath;

    private void DeleteCurrentTag()
    {
        if (_isReadOnly) return;

        if (!_editingTagId.HasValue)
        {
            UpdateStatus("Nothing to delete.");
            return;
        }

        var target = _project.FindTag(_editingTagId.Value);
        if (target is null)
        {
            UpdateStatus("Selected tag was not found.");
            RefreshTagList();
            SetWorkingTag(new Tag(), editingTagId: null);
            return;
        }

        var result = MessageBox.Show(
            this,
            $"タグ「{target.Label}」を削除しますか？\n(付与済みの全ステージ・ページからも解除されます)",
            "Delete Tag",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning);

        if (result != DialogResult.Yes) return;

        _project.RemoveTag(target.Id);

        RefreshTagList();
        SetWorkingTag(new Tag(), editingTagId: null);
        TagsChanged?.Invoke();
        UpdateStatus("Deleted.");
    }

    private void LoadSavedTag(Guid tagId)
    {
        var saved = _project.FindTag(tagId);
        if (saved is null)
        {
            UpdateStatus("Selected tag was not found.");
            RefreshTagList();
            return;
        }

        SetWorkingTag(saved, saved.Id);
    }

    private void RefreshTagList(Guid? selectId = null)
    {
        _tagListBox.Items.Clear();

        foreach (var tag in _project.Tags.OrderBy(t => t.Priority).ThenBy(t => t.Label))
        {
            _tagListBox.Items.Add(new TagListItem(tag.Id, tag.Label, ColorTranslator.FromHtml(tag.Color)));
        }

        if (selectId.HasValue)
        {
            for (var i = 0; i < _tagListBox.Items.Count; i++)
            {
                if (_tagListBox.Items[i] is TagListItem item && item.Id == selectId.Value)
                {
                    _tagListBox.SelectedIndex = i;
                    break;
                }
            }
        }
        else
        {
            _tagListBox.SelectedIndex = -1;
        }
    }

    /// <summary>
    /// ステータスラベルを更新する。
    /// </summary>
    /// <param name="message">表示するメッセージ。</param>
    private void UpdateStatus(string? message = null)
    {
        var mode = _editingTagId.HasValue ? "Editing" : "New / Unsaved";
        _statusLabel.Text = string.IsNullOrWhiteSpace(message) ? mode : $"{mode} - {message}";
    }

    /// <summary>
    /// アイコン画像をプロジェクトのBaseDirectory配下 TagIcons/ へコピーし、相対パスを返す。
    /// ファイル名衝突時は連番サフィックスを付与する（JsonProjectRepositoryの命名規則と同じ考え方）。
    /// </summary>
    private string CopyIconIntoProject(string sourcePath)
    {
        var iconDir = Path.Combine(_project.BaseDirectory, "TagIcons");
        Directory.CreateDirectory(iconDir);

        var fileName = Path.GetFileName(sourcePath);
        var candidate = Path.Combine(iconDir, fileName);

        // 既に同じ場所（TagIcons/内の既存アイコンを選び直した）ならコピー不要
        if (string.Equals(Path.GetFullPath(candidate), Path.GetFullPath(sourcePath), StringComparison.OrdinalIgnoreCase))
            return Path.GetRelativePath(_project.BaseDirectory, candidate);

        var nameOnly = Path.GetFileNameWithoutExtension(fileName);
        var ext = Path.GetExtension(fileName);
        var suffix = 2;

        while (File.Exists(candidate))
        {
            candidate = Path.Combine(iconDir, $"{nameOnly}_{suffix}{ext}");
            suffix++;
        }

        File.Copy(sourcePath, candidate, overwrite: false);
        return Path.GetRelativePath(_project.BaseDirectory, candidate);
    }

    /// <summary>
    /// タグリストの項目を表す内部クラス。
    /// </summary>
    private sealed class TagListItem
    {
        public Guid Id { get; }
        public string Label { get; }
        public Color Color { get; }

        public TagListItem(Guid id, string label, Color color)
        {
            Id = id;
            Label = label;
            Color = color;
        }

        public override string ToString() => Label;
    }

    /// <summary>
    /// 読み取り専用状態を反映する。Save/DeleteをガードするだけでOK
    /// （New/アイコン選択/色変更は作業コピー内の変更に過ぎず、Save時点で初めて確定するため）。
    /// </summary>
    /// <param name="isReadOnly">読み取り専用状態かどうか。</param>
    public void SetReadOnly(bool isReadOnly)
    {
        _isReadOnly = isReadOnly;
    }
}
