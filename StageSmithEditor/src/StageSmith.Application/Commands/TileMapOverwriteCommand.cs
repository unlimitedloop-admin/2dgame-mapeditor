using StageSmith.Core.Models;

namespace StageSmith.Application.Commands;

/// <summary>
/// ページのタイルマップを別のタイルマップで上書きするコマンド。
/// ノードエディタの複製モードで既存ページに貼り付けるときに使用する。
/// NodeX/NodeY・接続情報・RoomIdは変更しない。
/// </summary>
public class TileMapOverwriteCommand : ICommand
{
    private readonly Page     _targetPage;
    private readonly TileMap  _oldTileMap;
    private readonly TileMap  _newTileMap;
    private readonly Action   _onChanged;

    public TileMapOverwriteCommand(
        Page targetPage, TileMap oldTileMap, TileMap newTileMap, Action onChanged)
    {
        _targetPage = targetPage;
        _oldTileMap = oldTileMap;
        _newTileMap = newTileMap;
        _onChanged  = onChanged;
    }

    public void Execute()
    {
        _targetPage.TileMap = _newTileMap.Clone();
        _onChanged();
    }

    public void Undo()
    {
        _targetPage.TileMap = _oldTileMap.Clone();
        _onChanged();
    }
}
