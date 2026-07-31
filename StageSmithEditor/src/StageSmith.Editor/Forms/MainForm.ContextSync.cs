namespace StageSmith.Editor;

public partial class MainForm
{
    /// <summary>
    /// 現在のコンテキストに基づいてビューを更新する。
    /// </summary>
    private void ApplyContextToView()
    {
        var stage = _context.CurrentStage;
        var page = _context.CurrentPage;

        _page = page;

        _mapView.SetTileMap(page?.TileMap);
        _mapView.SetCurrentPageIndex(_context.CurrentPageIndex);
        _metaTilePalette.SetStage(stage);

        if (!string.IsNullOrWhiteSpace(stage?.TilesetImagePath))
        {
            // NOTE: ここでタイルセット画像をロードするのは、タイルセット画像が変更された場合にビューを更新するためです。
            // WARNING: タイルセット画像のロードは重い処理であるため、頻繁に呼び出すとパフォーマンスに影響を与える可能性があります。
            // REVIEW: ページを移動するたびに選択したタイルがリセットされるのはユーザーにとって不便であるため、タイルセット画像のロードを最小限に抑える方法を検討する必要があります。
            // TODO: 「今読み込まれているタイルセットがどのステージのものか」を覚えておいて、実際にステージが変わった時だけLoadTilesetImage()を呼ぶようにします。
            LoadTilesetImage(stage.TilesetImagePath);
        }

        _propertyWindow.RefreshProperties();
        SyncExplorerHighlight();

        _mapView.Invalidate();
        _tilePalette.Invalidate();
        _metaTilePalette.RefreshPalette();
    }

    /// <summary>
    /// エディタのコンテキストが変更されたときに呼び出される。
    /// </summary>
    private void OnEditorContextChanged()
    {
        ApplyContextToView();

        _stageExplorer.RebuildTree();
        SyncExplorerHighlight();

        _nodeEditorForm?.SyncPageSelection(_context.CurrentPageIndex);
    }
}
