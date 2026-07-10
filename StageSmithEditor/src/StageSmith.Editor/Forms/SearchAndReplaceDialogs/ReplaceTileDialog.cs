using StageSmith.Core.Constants;
using StageSmith.Core.Models;

namespace StageSmith.Editor.Forms;

/// <summary>
/// タイル置換ダイアログ（UI-09）。
/// 「置換」は現在ヒット中の1件のみ、「全て置換」はヒット全件
/// （「表示中のページのみ対象」チェックに従ったスコープ）を一括置換する。
/// </summary>
public sealed class ReplaceTileDialog : TileSearchDialogBase
{
    private readonly TextBox _replaceTileIdTextBox;
    private readonly Panel _replacePreviewPanel;
    private readonly CheckBox _scopeCurrentPageOnlyCheckBox;
    private readonly Button _replaceButton;
    private readonly Button _replaceAllButton;

    /// <summary>「置換」ボタン押下時に発火する。現在ヒットしている1件のみを置換する。</summary>
    public event Action<int>? ReplaceRequested;

    /// <summary>「全て置換」ボタン押下時に発火する。スコープ内のヒット全件を置換する。</summary>
    public event Action<int>? ReplaceAllRequested;

    private const int TopOffset = 60;         // 置換タイル番号行の分の余白
    private const int ButtonRowStartX = 172;  // 置換/全て置換ボタンの分の余白

    public ReplaceTileDialog(TileSearchState searchState, int initialTileId, Bitmap? tileset)
        : base(searchState, initialTileId, TopOffset, ButtonRowStartX)
    {
        Text = "タイル置換";
        ClientSize = new Size(540, 230);

        var replaceLabel = new Label
        {
            Text = "置換するタイル番号",
            Location = new Point(12, 64),
            AutoSize = true
        };

        _replaceTileIdTextBox = new TextBox
        {
            Location = new Point(12, 84),
            Width = 80,
            Text = searchState.ReplaceTileId >= 0 ? searchState.ReplaceTileId.ToString() : string.Empty
        };
        _replaceTileIdTextBox.TextChanged += (_, _) =>
        {
            if (TryGetTileId(_replaceTileIdTextBox, out var id))
                searchState.ReplaceTileId = id;
        };

        _replacePreviewPanel = new Panel
        {
            Location = new Point(280, 70),
            Size = new Size(ViewerConstants.TileRenderSize, ViewerConstants.TileRenderSize),
            BorderStyle = BorderStyle.FixedSingle
        };
        SetupTilePreview(_replacePreviewPanel, _replaceTileIdTextBox);

        _scopeCurrentPageOnlyCheckBox = new CheckBox
        {
            Text = "表示中のページのみ対象",
            Location = new Point(220, TopOffset + 70),
            AutoSize = true,
            Checked = searchState.ScopeCurrentPageOnly
        };
        _scopeCurrentPageOnlyCheckBox.CheckedChanged += (_, _) =>
            searchState.ScopeCurrentPageOnly = _scopeCurrentPageOnlyCheckBox.Checked;

        _replaceButton = new Button
        {
            Text = "置換",
            Location = new Point(12, TopOffset + 124),
            Width = 70
        };
        _replaceButton.Click += (_, _) => TryRaiseReplace(single: true);

        _replaceAllButton = new Button
        {
            Text = "全て置換",
            Location = new Point(88, TopOffset + 124),
            Width = 76
        };
        _replaceAllButton.Click += (_, _) => TryRaiseReplace(single: false);

        Controls.AddRange(new Control[]
        {
            replaceLabel, _replaceTileIdTextBox, _replacePreviewPanel,
            _scopeCurrentPageOnlyCheckBox, _replaceButton, _replaceAllButton
        });

        SetTileset(tileset);
    }

    private void TryRaiseReplace(bool single)
    {
        if (!TryGetTileId(_replaceTileIdTextBox, out var replaceTileId))
        {
            MessageBox.Show(
                "0～255の範囲で置換するタイル番号を入力してください。",
                Text,
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        if (single)
            ReplaceRequested?.Invoke(replaceTileId);
        else
            ReplaceAllRequested?.Invoke(replaceTileId);
    }

    public override void SetTileset(Bitmap? tileset)
    {
        base.SetTileset(tileset);
        _replacePreviewPanel.Invalidate();
    }
}
