using StageSmith.Core.Constants;
using StageSmith.Core.Models;

namespace StageSmith.Application.Commands;

/// <summary>
/// ノードエディタ上で隣接方向に新規ページを追加するコマンド。
/// 新規ページの追加と双方向接続の設定をまとめてUndo/Redo対応にする。
/// </summary>
public class NodeAddPageCommand : ICommand
{
    private readonly Stage      _stage;
    private readonly Page       _newPage;
    private readonly Page       _sourcePage;

    // 接続変更前のヘッダーを保存（Undo時に復元するため）
    private readonly PageHeader _sourceHeaderBefore;
    private readonly PageHeader _newPageHeaderAfter;

    private readonly Action _onChanged;

    public NodeAddPageCommand(
        Stage       stage,
        Page        sourcePage,
        Page        newPage,
        PageHeader  sourceHeaderAfter,
        Action      onChanged)
    {
        _stage              = stage;
        _sourcePage         = sourcePage;
        _newPage            = newPage;
        _sourceHeaderBefore = sourcePage.Header;       // Execute前の接続情報を保存
        _newPageHeaderAfter = sourceHeaderAfter;       // Execute後のsourceページヘッダー
        _onChanged          = onChanged;
    }

    public void Execute()
    {
        // 新規ページ追加
        if (!_stage.Pages.Contains(_newPage))
            _stage.Pages.Add(_newPage);

        // sourceページの接続を更新
        _sourcePage.Header = _newPageHeaderAfter;

        _onChanged();
    }

    public void Undo()
    {
        // sourceページの接続を元に戻す
        _sourcePage.Header = _sourceHeaderBefore;

        // 新規ページを削除
        _stage.Pages.Remove(_newPage);

        _onChanged();
    }
}
