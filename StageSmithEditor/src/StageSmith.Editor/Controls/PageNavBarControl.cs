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

    //========================
    // Controls
    //========================
    private readonly Button _firstButton;
    private readonly Button _prevButton;
    private readonly Label _pageLabel;
    private readonly Button _nextButton;
    private readonly Button _lastButton;

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

        _firstButton = CreateNavButton("|◀", "最初のページ (Ctrl+←)");
        _prevButton = CreateNavButton("◀", "前のページ (Ctrl+←)");
        _pageLabel = new Label
        {
            TextAlign = ContentAlignment.MiddleCenter,
            ForeColor = Color.White,
            Font = new Font("Yu Gothic UI", 9f),
            AutoSize = false,
            Width = 90
        };
        _nextButton = CreateNavButton("▶", "次のページ (Ctrl+→)");
        _lastButton = CreateNavButton("▶|", "最後のページ (Ctrl+→)");

        // イベント
        _firstButton.Click += (_, _) => NavRequested?.Invoke(NavAction.First);
        _prevButton.Click += (_, _) => NavRequested?.Invoke(NavAction.Prev);
        _nextButton.Click += (_, _) => NavRequested?.Invoke(NavAction.Next);
        _lastButton.Click += (_, _) => NavRequested?.Invoke(NavAction.Last);

        // レイアウト
        var panel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            AutoSize = false,
            BackColor = Color.Transparent,
            Padding = new Padding(4, 2, 4, 2)
        };

        panel.Controls.Add(_firstButton);
        panel.Controls.Add(_prevButton);
        panel.Controls.Add(_pageLabel);
        panel.Controls.Add(_nextButton);
        panel.Controls.Add(_lastButton);

        Controls.Add(panel);
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
