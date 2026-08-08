using StageSmith.Editor.Controllers;
using StageSmith.Editor.Controls;
using StageSmith.Editor.Forms;

namespace StageSmith.Editor;

public partial class MainForm
{
    // ========================
    // 隣接ページ移動
    // ========================
    private void NavigateAdjacentRoom(Direction direction)
    {
        var page = _context.CurrentPage;
        if (page == null) return;

        var targetRoomId = direction switch
        {
            Direction.Right => page.Header.RightPage,
            Direction.Left  => page.Header.LeftPage,
            Direction.Up    => page.Header.UpPage,
            Direction.Down  => page.Header.DownPage,
            _               => (byte)0xFF,
        };

        if (targetRoomId == 0xFF) return;

        var stage = _context.CurrentStage;
        if (stage == null) return;

        var targetIndex = stage.Pages.FindIndex(p => p.Header.RoomId == targetRoomId);

        if (targetIndex < 0) return;

        MoveToPageIndex(targetIndex);
    }

    // ========================
    // Zレイヤー移動
    // ========================
    private void NavigateZLayer(bool forward)
    {
        var stage = _context.CurrentStage;
        var currentPage = _context.CurrentPage;

        if (stage == null || currentPage == null)
            return;

        var currentZ = currentPage.Header.Z;

        var candidates = stage.Pages
            .Select((page, index) => new { Page = page, Index = index })
            .Where(x =>
                x.Page.NodeX == currentPage.NodeX &&
                x.Page.NodeY == currentPage.NodeY &&
                x.Page.Id != currentPage.Id);

        var target = forward
            ? candidates
                .Where(x => x.Page.Header.Z > currentZ)
                .OrderBy(x => x.Page.Header.Z)
                .FirstOrDefault()
            : candidates
                .Where(x => x.Page.Header.Z < currentZ)
                .OrderByDescending(x => x.Page.Header.Z)
                .FirstOrDefault();

        if (target == null)
            return;

        MoveToPageIndex(target.Index);
    }

    private bool CanMoveZLayer(bool forward)
    {
        var stage = _context.CurrentStage;
        var currentPage = _context.CurrentPage;

        if (stage == null || currentPage == null)
            return false;

        var currentZ = currentPage.Header.Z;

        return stage.Pages.Any(page =>
            page.NodeX == currentPage.NodeX &&
            page.NodeY == currentPage.NodeY &&
            page.Id != currentPage.Id &&
            (
                forward
                    ? page.Header.Z > currentZ
                    : page.Header.Z < currentZ
            ));
    }

    //========================
    // PageNavBar バインド
    //========================
    private void BindPageNavigationController()
    {
        _pageNavigationController = new PageNavigationController(
            _context,
            _pageNavBar,
            _mapView,
            _commandManager
        );

        _pageNavBar.ZRequested += NavigateToZ;
        _pageNavBar.JumpRequested += OpenJumpPageDialog;
        _pageNavBar.UpdateDisplay(_context);

        _context.ContextChanged += OnEditorContextChanged;

        _pageNavigationController.Refresh();
    }

    private void OpenJumpPageDialog()
    {
        var stage = _context.CurrentStage;
        if (stage == null || stage.Pages.Count == 0) return;

        var currentPageNumber = _context.CurrentPageIndex + 1;
        var maxPageNumber = stage.Pages.Count;

        using var dialog = new JumpPageDialog(currentPageNumber, maxPageNumber, _config.NumberDisplayFormat);
        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        var targetIndex = dialog.SelectedPageNumber - 1;
        if (targetIndex < 0 || targetIndex >= stage.Pages.Count) return;

        MoveToPageIndex(targetIndex);
    }

    private void NavigateToZ(int targetZ)
    {
        var stage = _context.CurrentStage;
        var currentPage = _context.CurrentPage;

        if (stage == null || currentPage == null)
            return;

        var targetIndex = stage.Pages.FindIndex(page =>
            page.NodeX == currentPage.NodeX &&
            page.NodeY == currentPage.NodeY &&
            page.Header.Z == targetZ);

        if (targetIndex < 0)
        {
            // 存在しないZを指定した場合は現在値へ戻す
            _pageNavBar.UpdateDisplay(_context);
            return;
        }

        MoveToPageIndex(targetIndex);
    }

    private void MoveToPageIndex(int pageIndex)
    {
        _context.SetPage(pageIndex);
        _pageNavBar.UpdateDisplay(_context);
    }

    // ========================
    // メニューコマンドハンドラ
    // ========================
    private void NavigateBack() => NavigateZLayer(forward: false);
    private void NavigateForward() => NavigateZLayer(forward: true);

    /// <summary>
    /// ページ移動を実行し、ビューを更新する。
    /// ナビゲーションバーのボタンとキーショートカットの両方から呼ばれる。
    /// </summary>
    public void NavigatePage(NavAction action) => _pageNavigationController?.Navigate(action);
}
