using StageSmith.Core.Models;

namespace StageSmith.Editor;

/// <summary>
/// 候補位置に割り当てる既存ページを選択するダイアログ。
/// ノードエディタの候補位置右クリック「部屋の割り当て」から開く。
/// </summary>
public class AssignPageDialog : Form
{
    private readonly Stage _stage;
    private readonly int   _filterZ;
    private readonly Point _targetPos;

    public int SelectedPageIndex { get; private set; } = -1;

    private readonly ListBox _listBox;

    public AssignPageDialog(Stage stage, int filterZ, Point targetPos)
    {
        _stage     = stage;
        _filterZ   = filterZ;
        _targetPos = targetPos;

        Text            = $"部屋の割り当て  →  ({targetPos.X}, {targetPos.Y}, z:{filterZ})";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition   = FormStartPosition.CenterParent;
        MaximizeBox     = false;
        MinimizeBox     = false;
        Size            = new Size(360, 320);

        var layout = new TableLayoutPanel
        {
            Dock        = DockStyle.Fill,
            RowCount    = 3,
            ColumnCount = 1,
            Padding     = new Padding(12),
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));

        var infoLabel = new Label
        {
            Text      = "割り当てるページを選択してください",
            Dock      = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Font      = new Font("Yu Gothic UI", 9f),
        };

        _listBox = new ListBox
        {
            Dock          = DockStyle.Fill,
            Font          = new Font("Yu Gothic UI", 9f),
            SelectionMode = SelectionMode.One,
        };

        BuildList();

        _listBox.DoubleClick += (_, _) => TryConfirm();

        var buttonPanel = new FlowLayoutPanel
        {
            Dock          = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents  = false,
            Padding       = new Padding(0, 6, 0, 0),
        };

        var cancelButton = new Button
        {
            Text         = "キャンセル",
            Width        = 90,
            Height       = 30,
            DialogResult = DialogResult.Cancel,
        };

        var okButton = new Button
        {
            Text         = "割り当て",
            Width        = 90,
            Height       = 30,
            DialogResult = DialogResult.None,
        };

        okButton.Click += (_, _) => TryConfirm();

        buttonPanel.Controls.AddRange([cancelButton, okButton]);

        layout.Controls.Add(infoLabel,    0, 0);
        layout.Controls.Add(_listBox,     0, 1);
        layout.Controls.Add(buttonPanel,  0, 2);

        Controls.Add(layout);
        AcceptButton = okButton;
        CancelButton = cancelButton;
    }

    private void BuildList()
    {
        // 全ページを表示（既に配置済みのページも選択可能にする）
        for (var i = 0; i < _stage.Pages.Count; i++)
        {
            var page = _stage.Pages[i];
            var h    = page.Header;
            _listBox.Items.Add(
                $"[{i}] Room ID: {h.RoomId}  {page.Name}  " +
                $"現在位置: ({page.NodeX}, {page.NodeY}, z:{h.Z})");
        }

        if (_listBox.Items.Count > 0)
            _listBox.SelectedIndex = 0;
    }

    private void TryConfirm()
    {
        if (_listBox.SelectedIndex < 0)
        {
            MessageBox.Show("ページを選択してください。",
                "未選択", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        SelectedPageIndex = _listBox.SelectedIndex;
        DialogResult      = DialogResult.OK;
        Close();
    }
}
