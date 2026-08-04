using StageSmith.Application.Commands;
using StageSmith.Core.Constants;
using StageSmith.Core.Models;
using StageSmith.Editor.Controls;

namespace StageSmith.Editor.Controllers;

public sealed class PageNavigationController
{
    private const byte NoRoom = 0xFF;

    private readonly EditorContext _context;
    private readonly PageNavBarControl _pageNavBar;
    private readonly MapViewControl _mapView;
    private readonly CommandManager _commandManager;

    public PageNavigationController(
        EditorContext context,
        PageNavBarControl pageNavBar,
        MapViewControl mapView,
        CommandManager commandManager)
    {
        _context = context;
        _pageNavBar = pageNavBar;
        _mapView = mapView;
        _commandManager = commandManager;

        _pageNavBar.NavRequested += OnPageNavRequested;
        _mapView.AdjacentNavigationRequested += OnAdjacentNavigationRequested;

        _context.ContextChanged += Refresh;
    }

    public void Refresh()
    {
        _pageNavBar.UpdateDisplay(_context);

        var page = _context.CurrentPage;
        if (page == null)
        {
            _mapView.SetAdjacentState(new MapAdjacentState());
            return;
        }

        _mapView.SetAdjacentState(new MapAdjacentState
        {
            Up = page.Header.UpPage,
            Down = page.Header.DownPage,
            Left = page.Header.LeftPage,
            Right = page.Header.RightPage
        });
    }

    public void Navigate(NavAction action)
    {
        switch (action)
        {
            case NavAction.First: _context.MoveFirstPage(); break;
            case NavAction.Prev:  _context.MovePrevPage();  break;
            case NavAction.Next:  _context.MoveNextPage();  break;
            case NavAction.Last:  _context.MoveLastPage();  break;
        }
    }

    private void OnPageNavRequested(NavAction action) => Navigate(action);

    private void OnAdjacentNavigationRequested(
        object? sender,
        AdjacentNavigationRequestedEventArgs e)
    {
        if (e.HasAdjacentPage)
        {
            MoveToAdjacentPage(e.Direction);
            return;
        }

        CreateAdjacentPageAndMove(e.Direction);
    }

    private void MoveToAdjacentPage(PageDirection direction)
    {
        var currentPage = _context.CurrentPage;
        var stage = _context.CurrentStage;

        if (currentPage == null || stage == null)
            return;

        var roomId = GetAdjacentRoomId(currentPage.Header, direction);
        if (roomId == NoRoom)
            return;

        var targetIndex = stage.Pages.FindIndex(page => page.Header.RoomId == roomId);
        if (targetIndex < 0)
            return;

        _context.SetPage(targetIndex);
    }

    private void CreateAdjacentPageAndMove(PageDirection direction)
    {
        var currentPage = _context.CurrentPage;
        var stage = _context.CurrentStage;

        if (currentPage == null || stage == null)
            return;

        if (_commandManager.IsReadOnly)
        {
            MessageBox.Show(
                "読み取り専用モードのため実行できません。",
                "新規ページ作成",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        var confirmMessage =
            $"ページ{currentPage.Header.RoomId}の{ToJapaneseDirection(direction)}側に新規ページを増設します。よろしいですか？";

        var result = MessageBox.Show(
            confirmMessage,
            "新規ページ作成",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (result != DialogResult.Yes)
            return;

        var command = new CreateAdjacentPageCommand(stage, currentPage, direction, _context);
        _commandManager.Execute(command);

        var newIndex = stage.Pages.IndexOf(command.NewPage!);
        _context.SetPage(newIndex);
    }

    private static byte GetAdjacentRoomId(PageHeader header, PageDirection direction)
    {
        return direction switch
        {
            PageDirection.Up => header.UpPage,
            PageDirection.Down => header.DownPage,
            PageDirection.Left => header.LeftPage,
            PageDirection.Right => header.RightPage,
            _ => NoRoom
        };
    }

    private static string ToJapaneseDirection(PageDirection direction)
    {
        return direction switch
        {
            PageDirection.Up => "上",
            PageDirection.Down => "下",
            PageDirection.Left => "左",
            PageDirection.Right => "右",
            _ => ""
        };
    }
}
