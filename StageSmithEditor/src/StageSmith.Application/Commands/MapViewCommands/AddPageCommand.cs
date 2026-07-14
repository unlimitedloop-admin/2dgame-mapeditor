using StageSmith.Core.Models;

namespace StageSmith.Application.Commands;

/// <summary>ステージへの新規ページ追加をUndo/Redo対応にするコマンド。</summary>
public sealed class AddPageCommand : ICommand
{
    private readonly Stage _stage;
    private readonly string? _name;
    private Page? _page;
    private int _insertIndex;

    public Page? AddedPage => _page;

    public AddPageCommand(Stage stage, string? name = null)
    {
        _stage = stage;
        _name = name;
    }

    public void Execute()
    {
        if (_page == null)
        {
            // 初回実行時のみ Stage.AddPage() で新規ページを生成する
            // （RoomId自動採番などはStage側の責務のまま）
            _page = _stage.AddPage(_name);
            _insertIndex = _stage.Pages.IndexOf(_page);
        }
        else
        {
            // Redo時は、初回に生成した同一インスタンスを同じ位置に戻す
            // （再度AddPage()を呼ぶと別のRoomIdを持つ別ページが生成されてしまうため）
            _stage.Pages.Insert(_insertIndex, _page);
        }
    }

    public void Undo()
    {
        if (_page == null) return;
        _stage.Pages.Remove(_page);
    }
}
