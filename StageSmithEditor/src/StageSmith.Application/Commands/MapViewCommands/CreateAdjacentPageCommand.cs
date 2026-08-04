using StageSmith.Core.Constants;
using StageSmith.Core.Models;

namespace StageSmith.Application.Commands;

/// <summary>
/// 現在ページに隣接する新規ページを作成し、双方向の接続(Header)を設定するコマンド。
/// マップビューの隣接ページ作成ボタン（＋マーク）から使用する。
/// </summary>
public sealed class CreateAdjacentPageCommand : ICommand
{
    private readonly Stage _stage;
    private readonly Page _currentPage;
    private readonly PageDirection _direction;
    private readonly EditorContext _context;

    private Page? _newPage;
    private int _insertIndex;
    private PageHeader _previousCurrentHeader;
    private int _selectedIndexBeforeExecute;

    public Page? NewPage => _newPage;

    public CreateAdjacentPageCommand(
        Stage stage,
        Page currentPage,
        PageDirection direction,
        EditorContext context)
    {
        _stage = stage;
        _currentPage = currentPage;
        _direction = direction;
        _context = context;
    }

    public void Execute()
    {
        _selectedIndexBeforeExecute = _context.CurrentPageIndex;

        if (_newPage == null)
        {
            _previousCurrentHeader = _currentPage.Header;

            EnsureRoomIds(_stage);
            var newRoomId = _stage.GetNextAvailableRoomId();

            _newPage = BuildAdjacentPage(_stage, _currentPage, _direction, newRoomId);

            _stage.Pages.Add(_newPage);
            _insertIndex = _stage.Pages.IndexOf(_newPage);
        }
        else
        {
            _stage.Pages.Insert(_insertIndex, _newPage);
        }

        ConnectBothWays(_currentPage, _newPage, _direction);

        _context.SetPage(_insertIndex);
    }

    public void Undo()
    {
        if (_newPage == null) return;

        _stage.Pages.Remove(_newPage);
        _currentPage.Header = _previousCurrentHeader;

        if (_stage.Pages.Count == 0) return;

        var restoreIndex = Math.Clamp(_selectedIndexBeforeExecute, 0, _stage.Pages.Count - 1);
        _context.SetPage(restoreIndex);
    }

    private static Page BuildAdjacentPage(
        Stage stage,
        Page currentPage,
        PageDirection direction,
        byte newRoomId)
    {
        var newPage = new Page
        {
            Name  = $"Page {newRoomId:D3}",
            NodeX = currentPage.NodeX + GetDeltaX(direction),
            NodeY = currentPage.NodeY + GetDeltaY(direction),
            Header = PageHeader.CreateDefault(),
        };

        var header = newPage.Header;
        header.RoomId = newRoomId;
        header.Z = currentPage.Header.Z;
        newPage.Header = header;

        return newPage;
    }

    private static void ConnectBothWays(Page currentPage, Page newPage, PageDirection direction)
    {
        var currentHeader = currentPage.Header;
        SetAdjacentRoomId(ref currentHeader, direction, newPage.Header.RoomId);
        currentPage.Header = currentHeader;

        var newHeader = newPage.Header;
        SetAdjacentRoomId(ref newHeader, GetOppositeDirection(direction), currentPage.Header.RoomId);
        newPage.Header = newHeader;
    }

    private static void SetAdjacentRoomId(ref PageHeader header, PageDirection direction, byte roomId)
    {
        switch (direction)
        {
            case PageDirection.Up:    header.UpPage    = roomId; break;
            case PageDirection.Down:  header.DownPage  = roomId; break;
            case PageDirection.Left:  header.LeftPage  = roomId; break;
            case PageDirection.Right: header.RightPage = roomId; break;
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

    private static int GetDeltaX(PageDirection direction) =>
        direction switch { PageDirection.Left => -1, PageDirection.Right => 1, _ => 0 };

    private static int GetDeltaY(PageDirection direction) =>
        direction switch { PageDirection.Up => -1, PageDirection.Down => 1, _ => 0 };

    private static void EnsureRoomIds(Stage stage)
    {
        foreach (var page in stage.Pages)
        {
            if (page.Header.RoomId != 0xFF)
                continue;

            var header = page.Header;
            header.RoomId = stage.GetNextAvailableRoomId();
            page.Header = header;
        }
    }
}
