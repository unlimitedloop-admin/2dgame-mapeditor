namespace StageSmith.Editor;

public partial class MainForm
{
    //========================
    // StageExplorer 初期化（イベント購読は起動時に一度だけ）
    //========================
    private void InitializeStageExplorerEvents()
    {
        // ページ選択 → MapView に反映
        _stageExplorer.PageSelected += (stage, page) =>
        {
            if (_context.Project == null) return;

            _context.SetStage(_context.Project.Stages.IndexOf(stage));
            _context.SetPage(stage.Pages.IndexOf(page));
        };

        _stageExplorer.StageListChanged += () =>
        {
            _propertyWindow.RefreshProperties();
        };

        _stageExplorer.PageListChanged += _ =>
        {
            _propertyWindow.RefreshProperties();
        };

        // 現在表示中のページが削除されたとき → 直前 or 直後 or 空表示
        _stageExplorer.PageDeleted += (stage, nextPage) =>
        {
            if (_context.Project == null) return;

            var stageIndex = _context.Project.Stages.IndexOf(stage);
            _context.SetStage(stageIndex);

            if (nextPage != null)
            {
                var pageIndex = stage.Pages.IndexOf(nextPage);
                _context.SetPage(pageIndex);
            }
            else
            {
                // ページが0件になった場合は空表示
                _page = null;
                _mapView.SetTileMap(null);
                _propertyWindow.RefreshProperties();
                _pageNavBar.UpdateDisplay(_context);
                _mapView.Invalidate();
            }
        };
    }

    //========================
    // StageExplorer バインド（プロジェクト切り替えの度に呼ぶ）
    //========================
    private void BindStageExplorer()
    {
        // _context.Project が null の場合（CloseProject）は
        // StageExplorerControl.Bind(null) 側で空表示にリセットされる
        _stageExplorer.Bind(_context.Project);
        SyncExplorerHighlight();
    }

    /// <summary>
    /// 現在の EditorContext に合わせて StageExplorer のハイライトを更新する。
    /// </summary>
    private void SyncExplorerHighlight()
    {
        var stage = _context.CurrentStage;
        var page = _context.CurrentPage;

        if (stage != null && page != null)
            _stageExplorer.SetCurrentPage(stage, page);
    }
}
