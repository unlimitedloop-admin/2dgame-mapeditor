using StageSmith.Core.Constants;
using StageSmith.Core.Models;

namespace StageSmith.Application.Commands;

/// <summary>
/// ページの接続情報をすべてクリアするコマンド。
/// Undo時は元の接続情報に戻す。
/// </summary>
public class NodeClearConnectionCommand : ICommand
{
    private readonly Page       _page;
    private readonly PageHeader _headerBefore;
    private readonly PageHeader _headerAfter;
    private readonly Action     _onChanged;

    public NodeClearConnectionCommand(Page page, Action onChanged)
    {
        _page         = page;
        _onChanged    = onChanged;
        _headerBefore = page.Header;

        // クリア後のヘッダーを事前計算
        var h = page.Header;
        h.LeftPage  = 0xFF;
        h.RightPage = 0xFF;
        h.UpPage    = 0xFF;
        h.DownPage  = 0xFF;
        h.FrontPage = 0xFF;
        h.BackPage  = 0xFF;
        _headerAfter = h;
    }

    public void Execute()
    {
        _page.Header = _headerAfter;
        _onChanged();
    }

    public void Undo()
    {
        _page.Header = _headerBefore;
        _onChanged();
    }
}
