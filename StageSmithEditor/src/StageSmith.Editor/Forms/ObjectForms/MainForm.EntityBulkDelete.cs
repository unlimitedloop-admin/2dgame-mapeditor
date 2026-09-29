using StageSmith.Application.Commands;
using StageSmith.Application.Services;
using StageSmith.Core.Models;

namespace StageSmith.Editor;

// 配置オブジェクトの一括削除専用partial。
// 入口は Stage Explorer（ページ単位・ステージ単位）と Object List（選択・一覧の結果すべて）。
// 件数の確認と Undo 登録はここに集約し、各入口は対象の列挙だけを行う。
// プレイヤー開始位置は対象外（Stage > Player Start の［クリア］で個別に消す）。
public partial class MainForm
{
    private const string BulkDeleteCaption = "オブジェクトの一括削除";

    private void BindEntityBulkDelete()
    {
        _stageExplorer.PageEntitiesDeleteRequested += (stage, page) =>
            DeleteEntities(stage, page.Entities.Select(e => (page, e)), $"ページ「{page.Name}」の配置オブジェクト");

        _stageExplorer.StageEntitiesDeleteRequested += stage =>
            DeleteEntities(stage, stage.EnumerateEntities(), $"ステージ「{stage.Name}」の配置オブジェクト");

        _objectList.DeleteRequested += (hits, allResults) =>
        {
            if (_context.CurrentStage is not { } stage) return;

            DeleteEntities(stage, hits.Select(h => (h.Page, h.Entity)),
                allResults ? "一覧の結果（絞り込み中のオブジェクト）" : "一覧で選択したオブジェクト");
        };
    }

    /// <summary>
    /// 件数を示して確認したうえで、対象のエンティティをまとめて削除する（Undo 1回で元に戻る）。
    /// </summary>
    private void DeleteEntities(Stage stage, IEnumerable<(Page Page, EntityPlacement Entity)> targets, string description)
    {
        if (BlockIfReadOnly(BulkDeleteCaption)) return;

        // 列挙元（page.Entities）を削除中に書き換えるため、先に確定させる
        var list = targets.ToList();

        if (list.Count == 0)
        {
            MessageBox.Show(this, $"{description}はありません。", BulkDeleteCaption,
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var confirm = MessageBox.Show(
            this,
            $"{description} {list.Count} 体を削除します。よろしいですか？\n（元に戻す（Ctrl+Z）で戻せます）",
            BulkDeleteCaption,
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning);

        if (confirm != DialogResult.Yes) return;

        _commandManager.Execute(new EntityBulkRemoveCommand(stage, list));

        // 選択中のエンティティを消した場合は選択を解除する（プロパティ表示も連動して消える）
        if (_objectTool?.SelectedEntityId is { } selectedId && list.Any(t => t.Entity.Id == selectedId))
            _objectTool.ClearSelection();

        ShowStatusMessage($"{description} {list.Count} 体を削除しました");
    }
}
