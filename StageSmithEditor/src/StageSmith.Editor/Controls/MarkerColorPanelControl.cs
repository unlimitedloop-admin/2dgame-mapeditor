namespace StageSmith.Editor.Controls;

/// <summary>
/// マーカーの色を設定するためのパネル。
/// マーカーは全体で1色のみ（BD-013仕様）。
/// </summary>
public sealed class MarkerColorPanelControl : UserControl
{
    private readonly Panel _colorSwatch;

    /// <summary>色が変更されたとき発火する。実際の反映（MapViewControlへの適用）は呼び出し側が行う。</summary>
    public event Action<Color>? ColorChanged;

    private Color _currentColor = Color.Red;

    public MarkerColorPanelControl()
    {
        Dock = DockStyle.Fill;
        Padding = new Padding(8);

        var layout = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            FlowDirection = FlowDirection.TopDown,
            AutoSize = true,
            WrapContents = false
        };

        var label = new Label
        {
            Text = "マーカーの色",
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 6)
        };

        _colorSwatch = new Panel
        {
            Width = 200,
            Height = 40,
            BackColor = _currentColor,
            BorderStyle = BorderStyle.FixedSingle,
            Margin = new Padding(0, 0, 0, 6)
        };

        var changeColorButton = new Button
        {
            Text = "色を変更...",
            Width = 200,
            Height = 28
        };
        changeColorButton.Click += (_, _) => OpenColorPicker();

        layout.Controls.Add(label);
        layout.Controls.Add(_colorSwatch);
        layout.Controls.Add(changeColorButton);

        Controls.Add(layout);
    }

    /// <summary>外部（MapViewControlの現在値）からの初期同期用。</summary>
    public void SetColor(Color color)
    {
        _currentColor = color;
        _colorSwatch.BackColor = color;
    }

    private void OpenColorPicker()
    {
        using var dialog = new ColorDialog
        {
            Color = _currentColor,
            FullOpen = true
        };

        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        _currentColor = dialog.Color;
        _colorSwatch.BackColor = _currentColor;
        ColorChanged?.Invoke(_currentColor);
    }
}
