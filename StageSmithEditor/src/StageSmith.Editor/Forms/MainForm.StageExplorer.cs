using StageSmith.Application.Commands;

namespace StageSmith.Editor;

public partial class MainForm
{
    //========================
    // StageExplorer 初期化（イベント購読は起動時に一度だけ）
    //========================
    private void InitializeStageExplorerEvents()
    {
        _stageExplorer.StageSelected += stage =>
        {
            if (_context.Project == null) return;
            _context.SetStage(_context.Project.Stages.IndexOf(stage));
        };

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

        _stageExplorer.PageDeleteRequested += (stage, page) =>
        {
            _commandManager.Execute(new RemovePageCommand(stage, page, _context));
        };

        _stageExplorer.StageDeleteRequested += DeleteStage;
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
