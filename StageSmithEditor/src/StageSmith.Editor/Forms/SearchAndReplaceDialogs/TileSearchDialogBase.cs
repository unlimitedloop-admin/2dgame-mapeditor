using StageSmith.Core.Constants;
using StageSmith.Core.Models;
using StageSmith.Editor.Utilities;

namespace StageSmith.Editor.Forms;

/// <summary>
/// タイル検索系ダイアログ（Find/Replace）の共通基盤。
/// タイル番号入力＋プレビュー、検索オプション（繰り返し・ハイライト）、
/// 件数表示、検索・次へ・前への操作を提供する。
/// 非モーダルで開いたままでもF4/Shift+F4で次へ/前へ操作できる。
/// </summary>
public abstract class TileSearchDialogBase : Form
{
    protected readonly TileSearchState _searchState;
    private readonly SafeTilesetHolder _tilesetHolder = new();
    protected readonly NumberDisplayFormat _format;

    protected readonly TextBox _tileIdTextBox;
    protected readonly Panel _previewPanel;
    protected readonly CheckBox _wrapAroundCheckBox;
    protected readonly CheckBox _showHighlightCheckBox;
    protected readonly Label _hitCountLabel;
    protected readonly Button _searchButton;
    protected readonly Button _nextButton;
    protected readonly Button _prevButton;
    protected readonly Button _cancelButton;

    /// <summary>「検索」ボタン押下（有効なタイルIDが確定した時）に発火する。</summary>
    public event Action<int>? SearchRequested;

    /// <summary>「次へ」ボタン、またはF4押下時に発火する。</summary>
    public event Action? NextRequested;

    /// <summary>「前へ」ボタン、またはShift+F4押下時に発火する。</summary>
    public event Action? PreviousRequested;

    /// <param name="topOffset">
    /// 派生クラスが検索タイル番号行の下に独自の行を挿入するための余白(px)。
    /// </param>
    /// <param name="buttonRowStartX">
    /// 検索／次へ／前へ／キャンセルボタン行の開始X座標。
    /// 派生クラスがボタン行の左側に独自ボタンを追加する場合に広げる。
    /// </param>
    protected TileSearchDialogBase(
        TileSearchState searchState,
        int initialTileId,
        NumberDisplayFormat format,
        int topOffset = 0,
        int buttonRowStartX = 12)
    {
        _searchState = searchState;
        _format = format;

        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;

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
            Text = initialTileId >= 0 ? NumberFormatHelper.FormatByte(initialTileId, format) : string.Empty
        };

        _previewPanel = new Panel
        {
            Location = new Point(280, 24),
            Size = new Size(ViewerConstants.TileRenderSize, ViewerConstants.TileRenderSize),
            BorderStyle = BorderStyle.FixedSingle
        };
        SetupTilePreview(_previewPanel, _tileIdTextBox);

        _wrapAroundCheckBox = new CheckBox
        {
            Text = "検索を繰り返す",
            Location = new Point(12, topOffset + 70),
            AutoSize = true,
            Checked = _searchState.WrapAround
        };
        _wrapAroundCheckBox.CheckedChanged += (_, _) =>
            _searchState.WrapAround = _wrapAroundCheckBox.Checked;

        _showHighlightCheckBox = new CheckBox
        {
            Text = "検索結果をマーカー表示",
            Location = new Point(12, topOffset + 94),
            AutoSize = true,
            Checked = _searchState.ShowHighlight
        };
        _showHighlightCheckBox.CheckedChanged += (_, _) =>
            _searchState.ShowHighlight = _showHighlightCheckBox.Checked;

        _hitCountLabel = new Label
        {
            Text = string.Empty,
            Location = new Point(160, topOffset + 96),
            AutoSize = true,
            ForeColor = SystemColors.GrayText
        };

        _searchButton = new Button
        {
            Text = "検索",
            Location = new Point(buttonRowStartX, topOffset + 124),
            Width = 70
        };
        _searchButton.Click += (_, _) => TrySearch();

        _prevButton = new Button
        {
            Text = "前へ",
            Location = new Point(buttonRowStartX + 76, topOffset + 124),
            Width = 60,
            Enabled = false
        };
        _prevButton.Click += (_, _) => PreviousRequested?.Invoke();

        _nextButton = new Button
        {
            Text = "次へ",
            Location = new Point(buttonRowStartX + 140, topOffset + 124),
            Width = 60,
            Enabled = false
        };
        _nextButton.Click += (_, _) => NextRequested?.Invoke();

        _cancelButton = new Button
        {
            Text = "キャンセル",
            Location = new Point(buttonRowStartX + 268, topOffset + 124),
            Width = 80
        };
        _cancelButton.Click += (_, _) => Close();

        Controls.AddRange(new Control[]
        {
            idLabel, _tileIdTextBox, _previewPanel,
            _wrapAroundCheckBox, _showHighlightCheckBox, _hitCountLabel,
            _searchButton, _prevButton, _nextButton, _cancelButton
        });

        AcceptButton = _searchButton;
        CancelButton = _cancelButton;
    }

    /// <summary>
    /// タイルID入力欄とプレビューパネルを連動させる。
    /// 派生クラスが独自のプレビュー（置換先タイル等）を追加する際にも使う。
    /// </summary>
    protected void SetupTilePreview(Panel previewPanel, TextBox tileIdTextBox)
    {
        tileIdTextBox.TextChanged += (_, _) => previewPanel.Invalidate();
        previewPanel.Paint += (_, e) => DrawTilePreview(e.Graphics, previewPanel, tileIdTextBox);
    }

    private void DrawTilePreview(Graphics g, Panel previewPanel, TextBox tileIdTextBox)
    {
        g.Clear(previewPanel.BackColor);

        var tileset = _tilesetHolder.Current;
        if (tileset == null || !TryGetTileId(tileIdTextBox, out var tileId))
            return;

        var srcSize = MapConstants.DefaultTileSize;
        var tilesPerRow = tileset.Width / srcSize;

        var sx = (tileId % tilesPerRow) * srcSize;
        var sy = (tileId / tilesPerRow) * srcSize;

        var srcRect = new Rectangle(sx, sy, srcSize, srcSize);
        var dstRect = previewPanel.ClientRectangle;

        g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
        g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;

        g.DrawImage(tileset, dstRect, srcRect, GraphicsUnit.Pixel);
    }

    /// <summary>指定テキストボックスから0～255のタイルIDをパースする（現在の表示形式に従う）。</summary>
    protected bool TryGetTileId(TextBox textBox, out int tileId)
    {
        if (!NumberFormatHelper.TryParseByte(textBox.Text, _format, out var value))
        {
            tileId = -1;
            return false;
        }

        tileId = value;
        return true;
    }

    private void TrySearch()
    {
        if (!TryGetTileId(_tileIdTextBox, out var tileId))
        {
            var hint = _format == NumberDisplayFormat.Hex ? "00～FFの16進数" : "0～255の数値";
            MessageBox.Show(
                $"{hint}でタイル番号を入力してください。",
                Text,
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        SearchRequested?.Invoke(tileId);
    }

    /// <summary>
    /// タイルセット画像が変更された場合に反映する（Import TileSet実行時など）。
    /// 独自プレビューを持つ派生クラスはオーバーライドしてそちらも更新すること。
    /// </summary>
    public virtual void SetTileset(Bitmap? tileset)
    {
        _tilesetHolder.Replace(tileset);
        _previewPanel.Invalidate();
    }

    /// <summary>ヒット件数表示を更新する（例: "3/12件"）。ヒットが無い場合は "0件"。</summary>
    public virtual void UpdateHitCount(int currentIndex, int totalCount)
    {
        _hitCountLabel.Text = totalCount > 0
            ? $"{currentIndex + 1}/{totalCount}件"
            : "0件";

        _nextButton.Enabled = totalCount > 0;
        _prevButton.Enabled = totalCount > 0;
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (keyData == Keys.F4)
        {
            NextRequested?.Invoke();
            return true;
        }

        if (keyData == (Keys.Shift | Keys.F4))
        {
            PreviousRequested?.Invoke();
            return true;
        }

        return base.ProcessCmdKey(ref msg, keyData);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _tilesetHolder.Dispose();
        }

        base.Dispose(disposing);
    }
}
