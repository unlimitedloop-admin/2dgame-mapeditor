using StageSmith.Core.Constants;
using StageSmith.Core.Models;

namespace StageSmith.Editor.Forms;

/// <summary>
/// タイル検索ダイアログ（UI-08）。
/// 非モーダルで表示し、「検索」ボタンを押すたびに次のヒットへ進む。
/// 検索の実行・ページジャンプ等の処理自体は持たず、SearchRequested イベントで外部に委譲する。
/// </summary>
public class FindTileDialog : Form
{
    private readonly TileSearchState _searchState;
    private Bitmap? _tileset;

    private readonly TextBox _tileIdTextBox;
    private readonly Panel _previewPanel;
    private readonly CheckBox _wrapAroundCheckBox;
    private readonly CheckBox _showHighlightCheckBox;
    private readonly Button _searchButton;
    private readonly Button _cancelButton;
    private readonly Label _hitCountLabel;

    /// <summary>
    /// 「検索」ボタンが押され、タイル番号が確定した際に発火する。
    /// </summary>
    public event Action<int>? SearchRequested;

    public FindTileDialog(TileSearchState searchState, int initialTileId, Bitmap? tileset)
    {
        _searchState = searchState;
        _tileset = tileset;

        Text = "タイル検索";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(360, 140);

        var idLabel = new Label
        {
            Text = "検索するタイル番号",
            Location = new Point(12, 18),
            AutoSize = true
        };

        _tileIdTextBox = new TextBox
        {
            Location = new Point(12, 38),
            Width = 80,
            Text = initialTileId >= 0 ? initialTileId.ToString() : string.Empty
        };
        _tileIdTextBox.TextChanged += (_, _) => UpdatePreview();

        _previewPanel = new Panel
        {
            Location = new Point(280, 24),
            Size = new Size(ViewerConstants.TileRenderSize, ViewerConstants.TileRenderSize),
            BorderStyle = BorderStyle.FixedSingle
        };
        _previewPanel.Paint += OnPreviewPaint;

        _wrapAroundCheckBox = new CheckBox
        {
            Text = "検索を繰り返す",
            Location = new Point(12, 70),
            AutoSize = true,
            Checked = _searchState.WrapAround
        };
        _wrapAroundCheckBox.CheckedChanged += (_, _) =>
            _searchState.WrapAround = _wrapAroundCheckBox.Checked;

        _showHighlightCheckBox = new CheckBox
        {
            Text = "検索結果をマーカー表示",
            Location = new Point(12, 94),
            AutoSize = true,
            Checked = _searchState.ShowHighlight
        };
        _showHighlightCheckBox.CheckedChanged += (_, _) =>
            _searchState.ShowHighlight = _showHighlightCheckBox.Checked;

        _hitCountLabel = new Label
        {
            Text = string.Empty,
            Location = new Point(160, 96),
            AutoSize = true,
            ForeColor = SystemColors.GrayText
        };

        _searchButton = new Button
        {
            Text = "検索",
            Location = new Point(180, 66),
            Width = 80
        };
        _searchButton.Click += OnSearchButtonClick;

        _cancelButton = new Button
        {
            Text = "キャンセル",
            Location = new Point(268, 66),
            Width = 80
        };
        _cancelButton.Click += (_, _) => Close();

        Controls.AddRange([
            idLabel, _tileIdTextBox, _previewPanel,
            _wrapAroundCheckBox, _showHighlightCheckBox, _hitCountLabel,
            _searchButton, _cancelButton
        ]);

        AcceptButton = _searchButton;
        CancelButton = _cancelButton;

        UpdatePreview();
    }

    /// <summary>
    /// タイルセット画像が変更された場合に反映する（Import TileSet実行時など）。
    /// </summary>
    public void SetTileset(Bitmap? tileset)
    {
        _tileset = tileset;
        _previewPanel.Invalidate();
    }

    /// <summary>
    /// ヒット件数表示を更新する（例: "3/12件"）。ヒットが無い場合は "0件"。
    /// </summary>
    public void UpdateHitCount(int currentIndex, int totalCount)
    {
        _hitCountLabel.Text = totalCount > 0
            ? $"{currentIndex + 1}/{totalCount}件"
            : "0件";
    }

    private void OnSearchButtonClick(object? sender, EventArgs e)
    {
        if (!TryGetTileId(out var tileId))
        {
            MessageBox.Show(
                "0～255の範囲でタイル番号を入力してください。",
                "タイル検索",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        SearchRequested?.Invoke(tileId);
    }

    private bool TryGetTileId(out int tileId)
    {
        return int.TryParse(_tileIdTextBox.Text, out tileId)
            && tileId >= 0
            && tileId <= 255;
    }

    private void UpdatePreview()
    {
        _previewPanel.Invalidate();
    }

    private void OnPreviewPaint(object? sender, PaintEventArgs e)
    {
        var g = e.Graphics;
        g.Clear(_previewPanel.BackColor);

        if (_tileset == null || !TryGetTileId(out var tileId))
            return;

        var srcSize = MapConstants.DefaultTileSize;
        var tilesPerRow = _tileset.Width / srcSize;

        var sx = (tileId % tilesPerRow) * srcSize;
        var sy = (tileId / tilesPerRow) * srcSize;

        var srcRect = new Rectangle(sx, sy, srcSize, srcSize);
        var dstRect = _previewPanel.ClientRectangle;

        g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
        g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;

        g.DrawImage(_tileset, dstRect, srcRect, GraphicsUnit.Pixel);
    }
}
