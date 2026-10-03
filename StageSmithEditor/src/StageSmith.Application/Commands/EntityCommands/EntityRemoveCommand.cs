using StageSmith.Core.Models;

namespace StageSmith.Application.Commands;

/// <summary>
/// ページからエンティティを1体削除するコマンド。Undo時は元の並び位置へ戻す
/// （.def の entities[] 出力順を変えないため）。
/// </summary>
public sealed class EntityRemoveCommand : ICommand
{
    private readonly Page _page;
    private readonly EntityPlacement _entity;
    private int _index = -1;

    public EntityRemoveCommand(Page page, EntityPlacement entity)
    {
        _page = page;
        _entity = entity;
    }

    public void Execute()
    {
        _index = _page.Entities.IndexOf(_entity);
        if (_index >= 0)
            _page.Entities.RemoveAt(_index);
    }

    public void Undo()
    {
        if (_index < 0 || _page.Entities.Contains(_entity)) return;

        _page.Entities.Insert(Math.Min(_index, _page.Entities.Count), _entity);
    }
}
