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

    public PageNavigationController(
        EditorContext context,
        PageNavBarControl pageNavBar,
        MapViewControl mapView)
    {
        _context = context;
        _pageNavBar = pageNavBar;
        _mapView = mapView;

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
            case NavAction.First:
                _context.MoveFirstPage();
                break;

            case NavAction.Prev:
                _context.MovePrevPage();
                break;

            case NavAction.Next:
                _context.MoveNextPage();
                break;

            case NavAction.Last:
                _context.MoveLastPage();
                break;
        }
    }

    private void OnPageNavRequested(NavAction action)
    {
        Navigate(action);
    }

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

        var targetIndex = stage.Pages.FindIndex(
            page => page.Header.RoomId == roomId);

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

        var confirmMessage =
            $"ページ{currentPage.Header.RoomId}の{ToJapaneseDirection(direction)}側に新規ページを増設します。よろしいですか？";

        var result = MessageBox.Show(
            confirmMessage,
            "新規ページ作成",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (result != DialogResult.Yes)
            return;

        EnsureRoomIds(stage);

        var newRoomId = GetNextAvailableRoomId(stage);
        var newPage = CreateAdjacentPage(stage, currentPage, direction, newRoomId);

        ConnectBothWays(currentPage, newPage, direction);

        var newIndex = stage.Pages.IndexOf(newPage);
        _context.SetPage(newIndex);
    }

    private static Page CreateAdjacentPage(
        Stage stage,
        Page currentPage,
        PageDirection direction,
        byte newRoomId)
    {
        var newPage = new Page
        {
            Name = $"Page {newRoomId:D3}",
            NodeX = currentPage.NodeX + GetDeltaX(direction),
            NodeY = currentPage.NodeY + GetDeltaY(direction),
            Header = PageHeader.CreateDefault(),
        };

        var header = newPage.Header;
        header.RoomId = newRoomId;
        header.Z = currentPage.Header.Z;
        newPage.Header = header;

        stage.Pages.Add(newPage);

        return newPage;
    }

    private static void ConnectBothWays(
        Page currentPage,
        Page newPage,
        PageDirection direction)
    {
        var currentHeader = currentPage.Header;
        SetAdjacentRoomId(
            ref currentHeader,
            direction,
            newPage.Header.RoomId);
        currentPage.Header = currentHeader;

        var newHeader = newPage.Header;
        SetAdjacentRoomId(
            ref newHeader,
            GetOppositeDirection(direction),
            currentPage.Header.RoomId);
        newPage.Header = newHeader;
    }

    private static byte GetAdjacentRoomId(
        PageHeader header,
        PageDirection direction)
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

    private static void SetAdjacentRoomId(
        ref PageHeader header,
        PageDirection direction,
        byte roomId)
    {
        switch (direction)
        {
            case PageDirection.Up:
                header.UpPage = roomId;
                break;

            case PageDirection.Down:
                header.DownPage = roomId;
                break;

            case PageDirection.Left:
                header.LeftPage = roomId;
                break;

            case PageDirection.Right:
                header.RightPage = roomId;
                break;
        }
    }

    private static PageDirection GetOppositeDirection(PageDirection direction)
    {
        return direction switch
        {
            PageDirection.Up => PageDirection.Down,
            PageDirection.Down => PageDirection.Up,
            PageDirection.Left => PageDirection.Right,
            PageDirection.Right => PageDirection.Left,
            _ => direction
        };
    }

    private static int GetDeltaX(PageDirection direction)
    {
        return direction switch
        {
            PageDirection.Left => -1,
            PageDirection.Right => 1,
            _ => 0
        };
    }

    private static int GetDeltaY(PageDirection direction)
    {
        return direction switch
        {
            PageDirection.Up => -1,
            PageDirection.Down => 1,
            _ => 0
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

    private static byte GetNextAvailableRoomId(Stage stage)
    {
        var used = stage.Pages
            .Select(page => page.Header.RoomId)
            .Where(roomId => roomId != NoRoom)
            .ToHashSet();

        for (var i = 0; i <= byte.MaxValue; i++)
        {
            var roomId = (byte)i;

            if (roomId == NoRoom)
                continue;

            if (!used.Contains(roomId))
                return roomId;
        }

        throw new InvalidOperationException("利用可能な RoomId がありません。");
    }

    private static void EnsureRoomIds(Stage stage)
    {
        foreach (var page in stage.Pages)
        {
            if (page.Header.RoomId != NoRoom)
                continue;

            var header = page.Header;
            header.RoomId = GetNextAvailableRoomId(stage);
            page.Header = header;
        }
    }
}
