using StageSmith.Core.Models;

namespace StageSmith.Application.Commands;

/// <summary>
/// ページにエンティティを1体配置するコマンド。
/// </summary>
public sealed class EntityPlaceCommand : ICommand
{
    private readonly Page _page;
    private readonly EntityPlacement _entity;

    public EntityPlacement Entity => _entity;

    public EntityPlaceCommand(Page page, EntityPlacement entity)
    {
        _page = page;
        _entity = entity;
    }

    public void Execute()
    {
        if (!_page.Entities.Contains(_entity))
            _page.Entities.Add(_entity);
    }

    public void Undo()
    {
        _page.Entities.Remove(_entity);
    }
}
