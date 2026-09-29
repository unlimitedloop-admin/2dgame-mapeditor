using StageSmith.Core.Models;

namespace StageSmith.Application.Commands;

/// <summary>
/// 複数のエンティティをまとめて削除するコマンド（Undo 1回で全て元に戻る）。
/// 個々の削除は EntityRemoveCommand に任せ、元の並び順（.def の出力順）も復元される。
///
/// NOTE: 未保存マークは通常 CommandManager.HistoryChanged で「表示中のステージ」にだけ付く。
/// Stage Explorer から表示していないステージのエンティティを消した場合も保存対象になるよう、
/// 実行時・Undo時に対象ステージへ自分で MarkDirty する。
/// </summary>
public sealed class EntityBulkRemoveCommand : ICommand
{
    private readonly Stage _stage;
    private readonly List<EntityRemoveCommand> _removes;

    public EntityBulkRemoveCommand(Stage stage, IEnumerable<(Page Page, EntityPlacement Entity)> targets)
    {
        _stage = stage;
        _removes = [.. targets.Select(t => new EntityRemoveCommand(t.Page, t.Entity))];
    }

    public int Count => _removes.Count;

    public void Execute()
    {
        foreach (var remove in _removes)
            remove.Execute();

        _stage.MarkDirty();
    }

    public void Undo()
    {
        // 削除時に記録した位置へ戻すため、逆順に戻す
        for (var i = _removes.Count - 1; i >= 0; i--)
            _removes[i].Undo();

        _stage.MarkDirty();
    }
}
