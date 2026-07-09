using System.Linq;
using StageSmith.Application.Services;
using StageSmith.Core.Models;
using StageSmith.Editor.Forms;

namespace StageSmith.Editor;

public partial class MainForm
{
    //========================
    // タイル検索
    //========================
    private readonly TileSearchState _searchState = new();
    private FindTileDialog? _findTileDialog;

    /// <summary>
    /// タイル検索まわりのイベント購読をまとめる初期化処理。
    /// コンストラクタから呼び出す。
    /// </summary>
    private void InitializeSearch()
    {
        // 検索状態の変更（新規検索・Next/Prev・クリア）→ ページ追従・ハイライト再描画・ダイアログ更新
        _searchState.Changed += OnSearchStateChanged;

        // 検索以外の経路（ページナビ・ノードエディタ経由など）でページが変わった場合も
        // 表示中ページに対応するヒットへハイライトを更新し直す
        _context.ContextChanged += () => RefreshSearchHighlightsOnMapView();
    }

    // ------------------------
    // メニュー／ショートカットから呼ばれるエントリポイント
    // ------------------------

    private void OpenFindTileDialog()
    {
        if (_findTileDialog is { IsDisposed: false })
        {
            _findTileDialog.Activate();
            return;
        }

        var initialTileId = _selectedTileId >= 0
            ? _selectedTileId
            : _searchState.TargetTileId;

        _findTileDialog = new FindTileDialog(_searchState, initialTileId, _tileset);
        _findTileDialog.SearchRequested   += ExecuteTileSearch;                  // 常に新規検索に単純化
        _findTileDialog.NextRequested     += () => _searchState.MoveNext();
        _findTileDialog.PreviousRequested += () => _searchState.MovePrevious();
        _findTileDialog.FormClosed        += (_, _) => _findTileDialog = null;

        _findTileDialog.UpdateHitCount(_searchState.CurrentIndex, _searchState.Hits.Count);
        _findTileDialog.Show(this);
    }

    private void FindTileOnMap()
    {
        // F3: パレット選択中のタイルIDで常に新規検索。次のヒットへ進みたい場合はF4を使う。
        if (_selectedTileId < 0) return;

        ExecuteTileSearch(_selectedTileId);
    }

    private void FindNextTile() => _searchState.MoveNext();

    private void FindPrevTile() => _searchState.MovePrevious();

    private void ClearSearchHighlight() => _searchState.Clear();

    // ------------------------
    // 検索実行本体
    // ------------------------

    private void ExecuteTileSearch(int tileId)
    {
        var stage = _context.CurrentStage;
        if (stage == null) return;

        var hits = TileSearchService.Search(stage, tileId);
        _searchState.SetHits(tileId, hits);

        // ダイアログが開いている場合は "0件" 表示で十分なので、
        // ダイアログ未表示（F3実行）のときだけメッセージで通知する
        if (!_searchState.HasHits && _findTileDialog is not { IsDisposed: false })
        {
            MessageBox.Show(
                $"タイル番号 {tileId} は見つかりませんでした。",
                "タイル検索",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
    }

    // ------------------------
    // 検索状態変更時の反映処理
    // ------------------------
    private void OnSearchStateChanged()
    {
        JumpToCurrentHitIfNeeded();
        RefreshSearchHighlightsOnMapView();

        _findTileDialog?.UpdateHitCount(_searchState.CurrentIndex, _searchState.Hits.Count);
    }

    private void JumpToCurrentHitIfNeeded()
    {
        if (_searchState.CurrentHit is not { } hit) return;

        if (_context.CurrentPageIndex != hit.PageIndex)
        {
            _context.SetPage(hit.PageIndex);
        }

        _mapViewContent.ScrollToTile(hit.X, hit.Y);
    }

    private void RefreshSearchHighlightsOnMapView()
    {
        var pageIndex = _context.CurrentPageIndex;
        var hits = _searchState.GetHitsForPage(pageIndex).ToList();

        var currentHit = _searchState.CurrentHit is { } hit && hit.PageIndex == pageIndex
            ? hit
            : (TileSearchHit?)null;

        _mapView.SetSearchHighlights(hits, currentHit, _searchState.ShowHighlight);
    }
}
