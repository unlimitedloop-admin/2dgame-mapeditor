using StageSmith.Core.Models;

namespace StageSmith.Editor;

/// <summary>
/// ステージ全体をゲーム同等の解像度で確認するための独立ウィンドウ。
/// 更新ボタンを押したときだけメインフォームの現在データを取り込む。
/// </summary>
public partial class StageMapViewerForm : Form
{
    private readonly EditorContext _context;
    private readonly Func<Bitmap?> _getTileset;

    private readonly StageMapViewerView _view;
    private readonly ComboBox _zComboBox;
    private readonly NumericUpDown _jumpPageBox;
    private readonly Button _jumpButton;
    private readonly Button _refreshButton;
    private readonly StatusStrip _statusStrip;
    private readonly ToolStripStatusLabel _centerLabel;
    private readonly ToolStripStatusLabel _zoomLabel;

    public StageMapViewerForm(EditorContext context, Func<Bitmap?> getTileset)
    {
        _context = context;
        _getTileset = getTileset;

        Text = "Stage Map Viewer";
        Size = new Size(900, 700);
        MinimumSize = new Size(500, 400);
        StartPosition = FormStartPosition.CenterScreen;
        KeyPreview = true;

        // --- 上部ツールバー ---
        var toolPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 32,
            FlowDirection = FlowDirection.LeftToRight,
            Padding = new Padding(4)
        };

        _refreshButton = new Button { Text = "更新", AutoSize = true };
        _refreshButton.Click += (_, _) => RefreshFromMainForm();

        _zComboBox = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 60 };
        _zComboBox.SelectedIndexChanged += (_, _) =>
        {
            if (_zComboBox.SelectedItem is int z)
            {
                _view?.FilterZ = z;
                _view?.Invalidate();
            }
        };

        _jumpPageBox = new NumericUpDown { Width = 60, Minimum = 0, Maximum = 999 };
        _jumpButton = new Button { Text = "中心へ", AutoSize = true };
        _jumpButton.Click += (_, _) => _view?.CenterOnPageIndex((int)_jumpPageBox.Value);

        toolPanel.Controls.AddRange([
            _refreshButton,
            new Label { Text = "Z:", AutoSize = true, TextAlign = ContentAlignment.MiddleLeft, Margin = new Padding(12, 6, 2, 0) },
            _zComboBox,
            new Label { Text = "Page:", AutoSize = true, TextAlign = ContentAlignment.MiddleLeft, Margin = new Padding(12, 6, 2, 0) },
            _jumpPageBox,
            _jumpButton,
        ]);

        // --- 中央ビュー ---
        _view = new StageMapViewerView { Dock = DockStyle.Fill };
        _view.CenterGridChanged += (x, y) => _centerLabel?.Text = $"中心座標: ({x}, {y})";
        _view.ZoomChanged += zoom => _zoomLabel?.Text = $"x{zoom:0.00}";

        // --- ステータスバー ---
        _statusStrip = new StatusStrip();
        _centerLabel = new ToolStripStatusLabel("中心座標: (-, -)") { Spring = true, TextAlign = ContentAlignment.MiddleLeft };
        _zoomLabel = new ToolStripStatusLabel("x1.00") { TextAlign = ContentAlignment.MiddleRight };
        _statusStrip.Items.Add(_centerLabel);
        _statusStrip.Items.Add(_zoomLabel);

        Controls.Add(_view);
        Controls.Add(toolPanel);
        Controls.Add(_statusStrip);

        Shown += (_, _) => RefreshFromMainForm();
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (keyData == Keys.F5)
        {
            RefreshFromMainForm();
            return true;
        }

        return base.ProcessCmdKey(ref msg, keyData);
    }

    private void RefreshFromMainForm()
    {
        var stage = _context.CurrentStage;
        RefreshZComboBox(stage);
        _view.RefreshFromStage(stage, _getTileset());
    }

    private void RefreshZComboBox(Stage? stage)
    {
        var zValues = (stage?.Pages.Select(p => (int)p.Header.Z).Distinct().OrderBy(zValue => zValue).ToList())
                      ?? [0];

        if (zValues.Count == 0) zValues.Add(0);

        var previous = _zComboBox.SelectedItem is int selectedZ ? selectedZ : (int?)null;

        _zComboBox.Items.Clear();
        foreach (var zValue in zValues)
            _zComboBox.Items.Add(zValue);

        _zComboBox.SelectedItem = previous.HasValue && zValues.Contains(previous.Value)
            ? previous.Value
            : zValues[0];
    }
}
