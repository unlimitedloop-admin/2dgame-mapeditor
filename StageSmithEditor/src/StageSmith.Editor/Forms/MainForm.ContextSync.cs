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
