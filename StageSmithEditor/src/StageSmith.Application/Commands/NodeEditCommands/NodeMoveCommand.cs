using StageSmith.Core.Models;

namespace StageSmith.Application.Commands;

/// <summary>
/// ノードエディタ上でページのNodeX/NodeYを移動するコマンド。
/// Ctrl+ドラッグで発生する。
/// </summary>
public class NodeMoveCommand : ICommand
{
    private readonly Page _page;
    private readonly int  _oldX, _oldY;
    private readonly int  _newX, _newY;
    private readonly Action _onChanged;

    public NodeMoveCommand(Page page, int newX, int newY, Action onChanged)
    {
        _page      = page;
        _oldX      = page.NodeX;
        _oldY      = page.NodeY;
        _newX      = newX;
        _newY      = newY;
        _onChanged = onChanged;
    }

    public void Execute()
    {
        _page.NodeX = _newX;
        _page.NodeY = _newY;
        _onChanged();
    }

    public void Undo()
    {
        _page.NodeX = _oldX;
        _page.NodeY = _oldY;
        _onChanged();
    }
}
