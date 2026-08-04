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

        var stageChanged = stage != null && stage.Id != _loadedTilesetStageId;
        var pageChanged = page?.Id != _lastAppliedPageId;
        _lastAppliedPageId = page?.Id;

        // ステージが実際に切り替わった時は、設定に関わらず必ずタイルセットを読み直す
        // （LoadTilesetImage / ClearTileset 内部で選択タイルのリセットも行われる）。
        if (stageChanged)
        {
            _loadedTilesetStageId = stage!.Id;

            if (!string.IsNullOrWhiteSpace(stage.TilesetImagePath))
            {
                LoadTilesetImage(stage.TilesetImagePath);
            }
            else
            {
                ClearTileset();
            }
        }
        // 同一ステージ内のページ移動時は、KeepSelectedTileOnPageChange 設定に従う。
        else if (pageChanged && !_config.KeepSelectedTileOnPageChange)
        {
            ResetTileSelectionOnly();
        }

        _propertyWindow.RefreshProperties();
        SyncExplorerHighlight();

        _mapView.Invalidate();
        _tilePalette.Invalidate();
        _metaTilePalette.RefreshPalette();
    }


    /// <summary>
    /// タイルセット自体は維持したまま、選択中タイルのみ解除する。
    /// KeepSelectedTileOnPageChange=OFF時の、同一ステージ内ページ移動用。
    /// </summary>
    private void ResetTileSelectionOnly()
    {
        _selectedTileId = -1;
        _tilePalette.SetSelected(-1);
        _mapView.PreviewTileId = -1;
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
