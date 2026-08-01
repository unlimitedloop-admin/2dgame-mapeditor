using StageSmith.Core.Constants;
using StageSmith.Core.Models;
using StageSmith.Editor.Utilities;

namespace StageSmith.Editor;

/// <summary>
/// 既存ページを隣接部屋として接続するダイアログ。
/// 選択中ページの周辺にある既存ページを候補として表示し、
/// 接続方向と接続先を選んで適用する。
/// </summary>
public class ConnectExistingPageDialog : Form
{
    //========================
    // 入力データ
    //========================
    private readonly Stage _stage;
    private readonly Page  _sourcePage;
    private readonly int   _sourceIndex;
    private readonly NumberDisplayFormat _format;

    //========================
    // 結果
    //========================
    public ConnectDirection SelectedDirection   { get; private set; } = ConnectDirection.Up;
    public int              SelectedTargetIndex { get; private set; } = -1;

    //========================
    // コントロール
    //========================
    // 方向ラジオ
    private readonly RadioButton _dirUp, _dirDown, _dirLeft, _dirRight, _dirBack, _dirFront;

    // 接続先ラジオ（候補 + 手動入力を同一グループに）
    // GroupBoxに全ラジオを入れることでWinFormsが同一グループとして扱う
    private readonly GroupBox _targetGroup;
    private readonly List<(RadioButton radio, int pageIndex)> _candidateRadios = [];
    private RadioButton? _radioManual;
    private TextBox? _manualRoomIdBox;

    //========================
    // 初期化
    //========================
    public ConnectExistingPageDialog(Stage stage, Page sourcePage, int sourceIndex, NumberDisplayFormat format)
    {
        _stage      = stage;
        _sourcePage = sourcePage;
        _sourceIndex = sourceIndex;
        _format      = format;

        Text            = "既存ページを隣接部屋として接続";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition   = FormStartPosition.CenterParent;
        MaximizeBox     = false;
        MinimizeBox     = false;
        Size            = new Size(440, 400);
        Padding         = new Padding(12);

        var layout = new TableLayoutPanel
        {
            Dock        = DockStyle.Fill,
            RowCount    = 4,
            ColumnCount = 1,
            Padding     = new Padding(8),
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));   // 選択中部屋情報
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 56));   // 方向ラジオ
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));   // 接続先グループ
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));   // ボタン

        // --- 選択中部屋情報 ---
        var sourceInfoLabel = new Label
        {
            Dock      = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Font      = new Font("Yu Gothic UI", 9f),
            Text      = $"選択中の Room ID: {NumberFormatHelper.FormatByte(sourcePage.Header.RoomId, format)}  " +
                        $"x:{sourcePage.NodeX}, y:{sourcePage.NodeY}, z:{sourcePage.Header.Z}",
        };

        // --- 方向ラジオボタン ---
        var dirGroup = new GroupBox
        {
            Text = "設定する隣接面",
            Dock = DockStyle.Fill,
            Font = new Font("Yu Gothic UI", 9f),
        };

        var dirFlow = new FlowLayoutPanel
        {
            Dock          = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents  = false,
            Padding       = new Padding(4, 2, 4, 2),
        };

        _dirUp    = CreateDirRadio("上",   true);
        _dirDown  = CreateDirRadio("下",   false);
        _dirLeft  = CreateDirRadio("左",   false);
        _dirRight = CreateDirRadio("右",   false);
        _dirBack  = CreateDirRadio("奥",   false);
        _dirFront = CreateDirRadio("手前", false);

        dirFlow.Controls.AddRange([_dirUp, _dirDown, _dirLeft, _dirRight, _dirBack, _dirFront]);
        dirGroup.Controls.Add(dirFlow);

        // --- 接続先グループ（候補 + 手動を同一GroupBoxに） ---
        // GroupBox内のRadioButtonは同一グループとして扱われる
        _targetGroup = new GroupBox
        {
            Text    = "接続先",
            Dock    = DockStyle.Fill,
            Font    = new Font("Yu Gothic UI", 9f),
            Padding = new Padding(8, 16, 8, 8),
        };

        BuildTargetGroup();

        // --- ボタン ---
        var buttonPanel = new FlowLayoutPanel
        {
            Dock          = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents  = false,
            Padding       = new Padding(0, 4, 0, 0),
        };

        var cancelButton = new Button
        {
            Text         = "キャンセル",
            Width        = 90,
            Height       = 30,
            DialogResult = DialogResult.Cancel,
        };

        var applyButton = new Button
        {
            Text         = "適用",
            Width        = 90,
            Height       = 30,
            DialogResult = DialogResult.None,
        };

        applyButton.Click += OnApplyClick;
        buttonPanel.Controls.AddRange([cancelButton, applyButton]);

        layout.Controls.Add(sourceInfoLabel, 0, 0);
        layout.Controls.Add(dirGroup,        0, 1);
        layout.Controls.Add(_targetGroup,    0, 2);
        layout.Controls.Add(buttonPanel,     0, 3);

        Controls.Add(layout);

        AcceptButton = applyButton;
        CancelButton = cancelButton;
    }

    //========================
    // 接続先グループ構築
    //========================
    private void BuildTargetGroup()
    {
        var candidates = FindCandidates();
        var yOffset    = 20;

        // 「もしかして？」ラベル
        if (candidates.Count > 0)
        {
            var suggestionLabel = new Label
            {
                Text      = "もしかして？",
                AutoSize  = true,
                Location  = new Point(8, yOffset),
                Font      = new Font("Yu Gothic UI", 9f, FontStyle.Bold),
                ForeColor = SystemColors.GrayText,
            };
            _targetGroup.Controls.Add(suggestionLabel);
            yOffset += 20;
        }

        // 候補ラジオ（GroupBox直下に配置 → 同一グループ）
        foreach (var (page, pageIndex) in candidates)
        {
            var radio = new RadioButton
            {
                AutoSize = true,
                Location = new Point(12, yOffset),
                Font     = new Font("Yu Gothic UI", 9f),
                Text     = $"Room ID  {NumberFormatHelper.FormatByte(page.Header.RoomId, _format)}     " +
                           $"x:{page.NodeX}, y:{page.NodeY}, z:{page.Header.Z}",
            };

            _targetGroup.Controls.Add(radio);
            _candidateRadios.Add((radio, pageIndex));
            yOffset += 24;
        }

        if (candidates.Count == 0)
        {
            var noCandidate = new Label
            {
                Text      = "（近くに候補がありません）",
                AutoSize  = true,
                Location  = new Point(12, yOffset),
                Font      = new Font("Yu Gothic UI", 9f),
                ForeColor = SystemColors.GrayText,
            };
            _targetGroup.Controls.Add(noCandidate);
            yOffset += 24;
        }

        yOffset += 8;

        // 区切り線代わりのラベル
        var manualLabel = new Label
        {
            Text      = "または他の部屋を指定",
            AutoSize  = true,
            Location  = new Point(8, yOffset),
            Font      = new Font("Yu Gothic UI", 9f, FontStyle.Bold),
            ForeColor = SystemColors.GrayText,
        };
        _targetGroup.Controls.Add(manualLabel);
        yOffset += 20;

        // 手動入力ラジオ（GroupBox直下 → 候補ラジオと同一グループ）
        _radioManual = new RadioButton
        {
            AutoSize = true,
            Location = new Point(12, yOffset),
            Font     = new Font("Yu Gothic UI", 9f),
            Text     = "Room ID",
        };

        _manualRoomIdBox = new TextBox
        {
            Location = new Point(90, yOffset - 2),
            Width    = 60,
            Enabled  = false,
            Font     = new Font("Yu Gothic UI", 9f),
        };

        _radioManual.CheckedChanged += (_, _) =>
            _manualRoomIdBox.Enabled = _radioManual.Checked;

        _targetGroup.Controls.Add(_radioManual);
        _targetGroup.Controls.Add(_manualRoomIdBox);

        // 候補が1件以上あれば先頭を選択
        if (_candidateRadios.Count > 0)
            _candidateRadios[0].radio.Checked = true;
        else
            _radioManual.Checked = true;
    }

    //========================
    // 候補収集
    //========================
    private List<(Page page, int pageIndex)> FindCandidates()
    {
        var results = new List<(Page, int)>();
        var sourceZ = _sourcePage.Header.Z;
        var sourceX = _sourcePage.NodeX;
        var sourceY = _sourcePage.NodeY;

        // ① 同Z・上下左右隣接
        foreach (var (dx, dy) in new[] { (0,-1),(0,1),(-1,0),(1,0) })
        {
            var tx = sourceX + dx;
            var ty = sourceY + dy;

            for (var i = 0; i < _stage.Pages.Count; i++)
            {
                if (i == _sourceIndex) continue;
                var p = _stage.Pages[i];
                if (p.NodeX == tx && p.NodeY == ty && p.Header.Z == sourceZ)
                {
                    results.Add((p, i));
                    if (results.Count >= 4) return results;
                }
            }
        }

        // ② 同XY・Z±1
        foreach (var dz in new[] { 1, -1 })
        {
            var tz = sourceZ + dz;
            for (var i = 0; i < _stage.Pages.Count; i++)
            {
                if (i == _sourceIndex) continue;
                var p = _stage.Pages[i];
                if (p.NodeX == sourceX && p.NodeY == sourceY && p.Header.Z == tz)
                {
                    results.Add((p, i));
                    if (results.Count >= 4) return results;
                }
            }
        }

        return results;
    }

    //========================
    // 適用ボタン
    //========================
    private void OnApplyClick(object? sender, EventArgs e)
    {
        SelectedDirection = GetSelectedDirection();

        if (_radioManual!.Checked == true)
        {
            if (!NumberFormatHelper.TryParseByte(_manualRoomIdBox!.Text, _format, out var roomId))
            {
                var hint = _format == NumberDisplayFormat.Hex ? "00〜FFの16進数" : "0〜254の数値";
                MessageBox.Show($"Room IDは{hint}で入力してください。",
                    "入力エラー", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var idx = _stage.Pages.FindIndex(p => p.Header.RoomId == roomId);
            if (idx < 0)
            {
                MessageBox.Show($"Room ID {roomId} のページが見つかりません。",
                    "入力エラー", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            SelectedTargetIndex = idx;
        }
        else
        {
            var selected = _candidateRadios.FirstOrDefault(c => c.radio.Checked);
            if (selected == default)
            {
                MessageBox.Show("接続先を選択してください。",
                    "未選択", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            SelectedTargetIndex = selected.pageIndex;
        }

        DialogResult = DialogResult.OK;
        Close();
    }

    //========================
    // ヘルパー
    //========================
    private ConnectDirection GetSelectedDirection()
    {
        if (_dirDown.Checked)  return ConnectDirection.Down;
        if (_dirLeft.Checked)  return ConnectDirection.Left;
        if (_dirRight.Checked) return ConnectDirection.Right;
        if (_dirBack.Checked)  return ConnectDirection.Back;
        if (_dirFront.Checked) return ConnectDirection.Front;
        return ConnectDirection.Up;
    }

    private static RadioButton CreateDirRadio(string text, bool isChecked)
    {
        return new RadioButton
        {
            Text     = text,
            AutoSize = true,
            Checked  = isChecked,
            Padding  = new Padding(0, 2, 8, 0),
            Font     = new Font("Yu Gothic UI", 9f),
        };
    }
}

/// <summary>接続方向の定義（Back/Frontを含む拡張版）。</summary>
public enum ConnectDirection
{
    Up, Down, Left, Right, Back, Front,
}
