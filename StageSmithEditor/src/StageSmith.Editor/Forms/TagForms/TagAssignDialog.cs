using StageSmith.Core.Models;

namespace StageSmith.Editor.Forms;

/// <summary>
/// Stage/Page共通で使う、タグの付与状態を編集するダイアログ。
/// プロジェクト全体のタグ一覧から、チェックボックスで付与/解除を選ぶ。
/// </summary>
public sealed class TagAssignDialog : Form
{
    private readonly CheckedListBox _listBox = new();

    /// <summary>OKで確定した、チェック済みタグのId一覧。</summary>
    public List<Guid> SelectedTagIds { get; private set; } = [];

    public TagAssignDialog(IReadOnlyList<Tag> allTags, IEnumerable<Guid> assignedTagIds)
    {
        Text            = "タグを編集";
        StartPosition   = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MinimizeBox     = false;
        MaximizeBox     = false;
        ClientSize      = new Size(260, 320);

        var assignedSet = assignedTagIds.ToHashSet();

        _listBox.Dock = DockStyle.Top;
        _listBox.Height = 240;
        _listBox.CheckOnClick = true;

        if (allTags.Count == 0)
        {
            _listBox.Items.Add("（タグが未登録です。Tag Managerで作成してください）");
            _listBox.Enabled = false;
        }
        else
        {
            foreach (var tag in allTags.OrderBy(t => t.Priority).ThenBy(t => t.Label))
            {
                var index = _listBox.Items.Add(new TagCheckItem(tag.Id, tag.Label));
                if (assignedSet.Contains(tag.Id))
                    _listBox.SetItemChecked(index, true);
            }
        }

        var okButton = new Button
        {
            Text         = "OK",
            DialogResult = DialogResult.None,
            Location     = new Point(ClientSize.Width - 170, ClientSize.Height - 36),
            Size         = new Size(75, 26),
        };

        okButton.Click += (_, _) =>
        {
            SelectedTagIds = _listBox.CheckedItems
                .OfType<TagCheckItem>()
                .Select(item => item.Id)
                .ToList();

            DialogResult = DialogResult.OK;
            Close();
        };

        var cancelButton = new Button
        {
            Text         = "キャンセル",
            DialogResult = DialogResult.Cancel,
            Location     = new Point(ClientSize.Width - 85, ClientSize.Height - 36),
            Size         = new Size(75, 26),
        };

        Controls.Add(_listBox);
        Controls.Add(okButton);
        Controls.Add(cancelButton);

        AcceptButton = okButton;
        CancelButton = cancelButton;
    }

    /// <summary>
    /// タグのチェックボックス用のラッパークラス。
    /// </summary>
    private sealed class TagCheckItem
    {
        public Guid Id { get; }
        private readonly string _label;

        public TagCheckItem(Guid id, string label)
        {
            Id = id;
            _label = label;
        }

        public override string ToString() => _label;
    }
}
