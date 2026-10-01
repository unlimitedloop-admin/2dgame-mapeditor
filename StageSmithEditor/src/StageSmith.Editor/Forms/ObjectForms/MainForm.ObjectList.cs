using StageSmith.Application.Services;
using StageSmith.Core.Constants;

namespace StageSmith.Editor;

// 配置オブジェクトの検索・一覧パネル（Object List）専用partial。
// 一覧の検索・表示は ObjectListControl が行い、ここではマップ側との連携
// （結果のハイライト、選んだオブジェクトへのジャンプ、F4/Shift+F4）だけを受け持つ。
public partial class MainForm
{
    private void BindObjectList()
    {
        _objectList.HitActivated += JumpToEntity;
        _objectList.HitsChanged += RefreshEntitySearchHighlights;

        // ページ・ステージの切り替えで「現在のページのみ」の対象や一覧の中身が変わるため
        _context.ContextChanged += RefreshObjectList;

        // 一覧が見えていない間は検索し直しを省くので、表示された時点で最新にする。
        // 隠れたときはハイライトも消す（一覧が見えないのに黄色だけ残らないように）。
        _objectListContent.VisibleChanged += (_, _) =>
        {
            if (_objectListContent.Visible) RefreshObjectList(force: true);
            else RefreshEntitySearchHighlights();
        };
    }

    /// <summary>
    /// 一覧を現在のステージ・ページで検索し直す（配置の変更・Undo・ページ切替のたび呼ぶ）。
    /// 一覧が画面に出ていない間（閉じている・別タブの裏）は、無駄な再構築を避けるため何もしない。
    /// </summary>
    private void RefreshObjectList() => RefreshObjectList(force: false);

    private void RefreshObjectList(bool force)
    {
        if (!force && !_objectListContent.Visible) return;

        _objectList.Reload(_context.CurrentStage, _context.CurrentPageIndex);
    }

    private void RefreshEntitySearchHighlights()
    {
        if (_mapView.EntityLayer is not { } layer) return;

        layer.HighlightedEntityIds = _objectList.ShowHighlight && _objectListContent.Visible
            ? _objectList.Hits.Select(h => h.Entity.Id).ToHashSet()
            : [];

        _mapView.Invalidate();
    }

    /// <summary>
    /// 一覧で選んだオブジェクトのページへ移動し、オブジェクトツールで選択して見える位置までスクロールする。
    /// </summary>
    private void JumpToEntity(EntitySearchHit hit)
    {
        var stage = _context.CurrentStage;
        var pageIndex = stage?.Pages.IndexOf(hit.Page) ?? -1;
        if (pageIndex < 0) return;

        if (_context.CurrentPageIndex != pageIndex)
            _context.SetPage(pageIndex);

        _objectPalette.SetPlayerStartMode(false);
        SetToolMode(EditorToolMode.Object);

        _objectTool?.Select(hit.Entity);

        // 足元の少し上（スプライトの中ほど）が見えるようにスクロールする
        var tileX = hit.Entity.X / MapConstants.DefaultTileSize;
        var tileY = Math.Max(0, hit.Entity.Y - 1) / MapConstants.DefaultTileSize;
        _mapViewContent.ScrollToTile(tileX, tileY);
    }

    /// <summary>F4 / Shift+F4：オブジェクトツール使用中は Object List の次／前の結果へ移動する。</summary>
    private bool TryMoveObjectListSelection(bool forward)
    {
        if (_currentMode != EditorToolMode.Object) return false;

        // 一覧が隠れている間は検索し直しを省いているので、移動の前に最新にする
        if (!_objectListContent.Visible)
            RefreshObjectList(force: true);

        if (forward) _objectList.SelectNext();
        else _objectList.SelectPrevious();

        return true;
    }
}
