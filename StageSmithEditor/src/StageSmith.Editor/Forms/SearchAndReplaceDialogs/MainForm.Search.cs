using StageSmith.Application.Commands;
using StageSmith.Application.Services;
using StageSmith.Core.Models;
using StageSmith.Editor.Forms;
using StageSmith.Editor.Utilities;

namespace StageSmith.Editor;

public partial class MainForm
{
    //========================
    // タイル検索
    //========================
    private readonly TileSearchState _searchState = new();
    private FindTileDialog? _findTileDialog;

    //========================
    // タイル置換
    //========================
    private ReplaceTileDialog? _replaceTileDialog;

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

    /// <param name="tileId">
    /// 検索するタイル番号の欄に入れる値（マップの右クリック「このタイルを検索」から）。
    /// null の場合は従来どおり、パレットで選択中のタイル → 前回の検索対象の順で決める。
    /// </param>
    private void OpenFindTileDialog(int? tileId = null)
    {
        if (_findTileDialog is { IsDisposed: false })
        {
            if (tileId is { } id)
                _findTileDialog.SetSearchTileId(id);

            _findTileDialog.Activate();
            return;
        }

        var initialTileId = tileId ?? (_selectedTileId >= 0
            ? _selectedTileId
            : _searchState.TargetTileId);

        _findTileDialog = new FindTileDialog(_searchState, initialTileId, _tileset, _config.NumberDisplayFormat);
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

    /// <summary>
    /// タイルパレットで選んだタイルを、開いている検索／置換ダイアログの入力欄に反映する。
    /// </summary>
    private void ApplyPaletteTileToSearchDialogs(int tileId)
    {
        if (tileId < 0) return;

        if (_findTileDialog is { IsDisposed: false })
            _findTileDialog.ApplyPaletteTile(tileId);

        if (_replaceTileDialog is { IsDisposed: false })
            _replaceTileDialog.ApplyPaletteTile(tileId);
    }

    private void FindNextTile()
    {
        if (TryMoveObjectListSelection(forward: true)) return;
        _searchState.MoveNext();
    }

    private void FindPrevTile()
    {
        if (TryMoveObjectListSelection(forward: false)) return;
        _searchState.MovePrevious();
    }

    private void ClearSearchHighlight() => _searchState.Clear();

    // ------------------------
    // 検索実行本体
    // ------------------------
    private void ExecuteTileSearch(int tileId)
    {
        var stage = _context.CurrentStage;
        if (stage == null) return;

        var scopePageIndex = _searchState.ScopeCurrentPageOnly
            ? _context.CurrentPageIndex
            : (int?)null;

        var hits = TileSearchService.Search(stage, tileId, scopePageIndex);
        _searchState.SetHits(tileId, hits);

        if (_searchState.HasHits) return;

        // 見つからなかった場合：ダイアログを開いていればダイアログ内に表示し（メッセージボックスは重ねない）、
        // 開いていなければ（F3 での検索など）メッセージボックスで知らせる
        var dialogs = new TileSearchDialogBase?[] { _findTileDialog, _replaceTileDialog }
            .Where(d => d is { IsDisposed: false })
            .ToList();

        if (dialogs.Count > 0)
        {
            foreach (var dialog in dialogs)
                dialog!.ShowNotFound(tileId);
            return;
        }

        MessageBox.Show(
            $"タイル番号 {NumberFormatHelper.FormatByte(tileId, _config.NumberDisplayFormat)} は見つかりませんでした。",
            "タイル検索",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
    }

    // ------------------------
    // 検索状態変更時の反映処理
    // ------------------------
    private void OnSearchStateChanged()
    {
        JumpToCurrentHitIfNeeded();
        RefreshSearchHighlightsOnMapView();

        _findTileDialog?.UpdateHitCount(_searchState.CurrentIndex, _searchState.Hits.Count);
        _replaceTileDialog?.UpdateHitCount(_searchState.CurrentIndex, _searchState.Hits.Count);
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

    // ------------------------
    // 置換ダイアログ関連
    // ------------------------
    private void OpenReplaceTileDialog()
    {
        if (_replaceTileDialog is { IsDisposed: false })
        {
            _replaceTileDialog.Activate();
            return;
        }

        var initialTileId = _selectedTileId >= 0
            ? _selectedTileId
            : _searchState.TargetTileId;

        _replaceTileDialog = new ReplaceTileDialog(_searchState, initialTileId, _tileset, _config.NumberDisplayFormat);
        _replaceTileDialog.SearchRequested     += ExecuteTileSearch;
        _replaceTileDialog.NextRequested       += () => _searchState.MoveNext();
        _replaceTileDialog.PreviousRequested   += () => _searchState.MovePrevious();
        _replaceTileDialog.ReplaceRequested    += ReplaceCurrentHit;
        _replaceTileDialog.ReplaceAllRequested += ReplaceAllHits;
        _replaceTileDialog.FormClosed += (_, _) => _replaceTileDialog = null;

        _replaceTileDialog.UpdateHitCount(_searchState.CurrentIndex, _searchState.Hits.Count);
        _replaceTileDialog.Show(this);
    }

    private void ReplaceCurrentHit(int searchTileId, int replaceTileId)
    {
        EnsureSearchExecuted(searchTileId);

        if (_searchState.CurrentHit is not { } hit) return;

        var stage = _context.CurrentStage;
        if (stage == null) return;

        var tileMap = stage.Pages[hit.PageIndex].TileMap;
        var command = new TilePaintCommand(tileMap, [(hit.X, hit.Y)], (byte)replaceTileId);

        _commandManager.Execute(command);

        // 置換後は対象タイルが消えているはずなので、同条件で再検索して次のヒットへ進む
        ExecuteTileSearch(searchTileId);
    }

    private void ReplaceAllHits(int searchTileId, int replaceTileId)
    {
        EnsureSearchExecuted(searchTileId);

        if (!_searchState.HasHits) return;

        var stage = _context.CurrentStage;
        if (stage == null) return;

        var commands = _searchState.Hits
            .GroupBy(h => h.PageIndex)
            .Select(g => (ICommand)new TilePaintCommand(
                stage.Pages[g.Key].TileMap,
                g.Select(h => (h.X, h.Y)),
                (byte)replaceTileId))
            .ToList();

        if (commands.Count == 0) return;

        _commandManager.Execute(new CompositeCommand(commands));

        _searchState.Clear();
    }

    /// <summary>
    /// 現在の検索状態が指定タイルIDと一致していない、または未検索の場合、新規に検索を実行する。
    /// 「検索」ボタンを押さずに「置換」「全て置換」を押した場合の救済用。
    /// </summary>
    private void EnsureSearchExecuted(int searchTileId)
    {
        if (_searchState.TargetTileId == searchTileId && _searchState.HasHits)
            return;

        ExecuteTileSearch(searchTileId);
    }
}
