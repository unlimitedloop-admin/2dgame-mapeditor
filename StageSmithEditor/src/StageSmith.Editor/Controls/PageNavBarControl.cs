using StageSmith.Core.Models;

namespace StageSmith.Editor.Controls;

/// <summary>
/// マップビュー下部に表示するページナビゲーションバー。
/// [|◀] [◀] [ 1 / 12 ] [▶] [▶|]
/// </summary>
public class PageNavBarControl : UserControl
{
    //========================
    // イベント
    //========================
    /// <summary>ページ移動ボタンが押されたとき発火する。</summary>
    public event Action<NavAction>? NavRequested;

    /// <summary>Z座標変更（直接指定）が要求されたとき発火する。</summary>
    public event Action<int>? ZRequested;

    //========================
    // Controls
    //========================
    private readonly Button _firstButton;
    private readonly Button _prevButton;
    private readonly Label _pageLabel;
    private readonly Button _nextButton;
    private readonly Button _lastButton;
    private readonly NumericUpDown _zNumeric;
    private bool _updating;

    //========================
    // 定数
    //========================
    private const int ButtonWidth = 32;
    private const int BarHeight = 28;

    //========================
    // 初期化
    //========================
    public PageNavBarControl()
    {
        Height = BarHeight;
        Dock = DockStyle.Bottom;
        BackColor = SystemColors.ControlDark;

        _firstButton = CreateNavButton("|◀", "最初のページ");
        _prevButton = CreateNavButton("◀", "前のページ");
        _pageLabel = new Label
        {
            TextAlign = ContentAlignment.MiddleCenter,
            ForeColor = Color.White,
            Font = new Font("Yu Gothic UI", 9f),
            AutoSize = false,
            Width = 80
        };
        _nextButton = CreateNavButton("▶", "次のページ");
        _lastButton = CreateNavButton("▶|", "最後のページ");

        _firstButton.Click += (_, _) => NavRequested?.Invoke(NavAction.First);
        _prevButton.Click += (_, _) => NavRequested?.Invoke(NavAction.Prev);
        _nextButton.Click += (_, _) => NavRequested?.Invoke(NavAction.Next);
        _lastButton.Click += (_, _) => NavRequested?.Invoke(NavAction.Last);

        var leftPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Left,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            AutoSize = false,
            Width = ButtonWidth * 4 + _pageLabel.Width + 32,
            BackColor = Color.Transparent,
            Padding = new Padding(4, 2, 4, 2)
        };

        leftPanel.Controls.Add(_firstButton);
        leftPanel.Controls.Add(_prevButton);
        leftPanel.Controls.Add(_pageLabel);
        leftPanel.Controls.Add(_nextButton);
        leftPanel.Controls.Add(_lastButton);

        var zLabel = new Label
        {
            Text = "Z",
            ForeColor = Color.White,
            TextAlign = ContentAlignment.MiddleCenter,
            AutoSize = false,
            Width = 20,
            Height = BarHeight - 4,
            Font = new Font("Yu Gothic UI", 9f, FontStyle.Bold)
        };

        _zNumeric = new NumericUpDown
        {
            Minimum = 0,
            Maximum = 255,
            Width = 64,
            Height = BarHeight - 4,
            TextAlign = HorizontalAlignment.Right,
            TabStop = false
        };

        _zNumeric.ValueChanged += (_, _) =>
        {
            if (_updating)
                return;

            ZRequested?.Invoke((int)_zNumeric.Value);
        };

        var rightPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Right,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            AutoSize = false,
            Width = 96,
            BackColor = Color.Transparent,
            Padding = new Padding(4, 2, 4, 2)
        };

        rightPanel.Controls.Add(zLabel);
        rightPanel.Controls.Add(_zNumeric);

        Controls.Add(leftPanel);
        Controls.Add(rightPanel);
    }

    //========================
    // 公開メソッド
    //========================

    /// <summary>
    /// 現在のページ情報を反映してラベルを更新する。
    /// </summary>
    public void UpdateDisplay(EditorContext context)
    {
        var total = context.PageCount;
        var current = total > 0 ? context.CurrentPageIndex + 1 : 0;

        _pageLabel.Text = total > 0
            ? $"{current} / {total}"
            : "- / -";

        _firstButton.Enabled = current > 1;
        _prevButton.Enabled = current > 1;
        _nextButton.Enabled = current < total;
        _lastButton.Enabled = current < total;

        _updating = true;
        try
        {
            var page = context.CurrentPage;

            if (page == null)
            {
                _zNumeric.Enabled = false;
                _zNumeric.Value = 0;
            }
            else
            {
                _zNumeric.Enabled = true;

                var z = Math.Clamp((int)page.Header.Z, 0, 255);
                _zNumeric.Value = z;
            }
        }
        finally
        {
            _updating = false;
        }
    }

    //========================
    // ヘルパー
    //========================
    private static Button CreateNavButton(string text, string tooltip)
    {
        return new Button
        {
            Text = text,
            Width = ButtonWidth,
            Height = BarHeight - 4,
            FlatStyle = FlatStyle.Flat,
            ForeColor = Color.White,
            BackColor = Color.FromArgb(80, 80, 90),
            Font = new Font("Yu Gothic UI", 8f, FontStyle.Bold),
            TabStop = false,
            // ToolTip は外から設定できないので Tag に持たせておく
            Tag = tooltip
        };
    }
}

/// <summary>ナビゲーション操作の種類。</summary>
public enum NavAction
{
    First,
    Prev,
    Next,
    Last
}
