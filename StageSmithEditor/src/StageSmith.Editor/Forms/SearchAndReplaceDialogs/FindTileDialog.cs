using StageSmith.Core.Models;

namespace StageSmith.Editor.Forms;

/// <summary>タイル検索ダイアログ（UI-08）。</summary>
public sealed class FindTileDialog : TileSearchDialogBase
{
    public FindTileDialog(TileSearchState searchState, int initialTileId, Bitmap? tileset)
        : base(searchState, initialTileId)
    {
        Text = "タイル検索";
        ClientSize = new Size(380, 170);

        SetTileset(tileset);
    }
}
