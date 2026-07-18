using StageSmith.Application.Commands;
using StageSmith.Core.Models;

namespace StageSmith.Editor;

public partial class MainForm
{
    private readonly List<MetaTilePlaceCommand> _currentMetaTilePlaceCommands = [];
    private readonly HashSet<Point> _currentMetaTileDragOrigins = [];
    private bool _isMetaTileDragPlacing;
    private bool _metaTilePlacementInitialized;

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        InitializeMetaTilePlacement();
    }

    private void InitializeMetaTilePlacement()
    {
        if (_metaTilePlacementInitialized)
            return;

        _metaTilePlacementInitialized = true;

        // MetaTileブラシ中は、MapViewControl内のPenTool/SelectionToolには左クリック入力を渡さない。
        _mapView.ShouldSuppressToolInput = IsMetaTileBrushActive;

        // 既存のTilePalette選択とMetaTilePalette選択をブラシ状態として同期する。
        _tilePalette.TileSelected += tileId =>
        {
            _selectedTileId = tileId;
            _context.SetSelectedTile(tileId);
            _metaTilePalette.SetSelected(null);
        };

        _context.BrushChanged += RefreshMapViewBrushPreview;

        _mapView.MouseDown += OnMapViewMetaTileMouseDown;
        _mapView.MouseMove += OnMapViewMetaTileMouseMove;
        _mapView.MouseUp += OnMapViewMetaTileMouseUp;

        RefreshMapViewBrushPreview();
    }

    private bool IsMetaTileBrushActive()
    {
        return _context.CurrentBrushKind == EditorBrushKind.MetaTile
            && _context.SelectedMetaTile != null;
    }

    private void OnMapViewMetaTileMouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left)
            return;

        if (!CanPlaceMetaTile())
            return;

        BeginMetaTileDragPlace();
        TryPlaceMetaTileAt(e.X, e.Y);
    }

    private void OnMapViewMetaTileMouseMove(object? sender, MouseEventArgs e)
    {
        if (!_isMetaTileDragPlacing)
            return;

        if (e.Button != MouseButtons.Left)
            return;

        TryPlaceMetaTileAt(e.X, e.Y);
    }

    private void OnMapViewMetaTileMouseUp(object? sender, MouseEventArgs e)
    {
        if (!_isMetaTileDragPlacing)
            return;

        EndMetaTileDragPlace();
    }

    private bool CanPlaceMetaTile()
    {
        return _page?.TileMap != null
            && _context.CurrentBrushKind == EditorBrushKind.MetaTile
            && _context.SelectedMetaTile != null;
    }

    private void BeginMetaTileDragPlace()
    {
        _isMetaTileDragPlacing = true;
        _currentMetaTilePlaceCommands.Clear();
        _currentMetaTileDragOrigins.Clear();
    }

    private void TryPlaceMetaTileAt(int mouseX, int mouseY)
    {
        if (_page?.TileMap == null)
            return;

        var metaTile = _context.SelectedMetaTile;

        if (metaTile == null)
            return;

        if (!_mapView.TryScreenToTile(mouseX, mouseY, out var tileX, out var tileY))
            return;

        var origin = new Point(tileX, tileY);

        // 同じ原点に対してドラッグ中に何度も配置しない。
        if (!_currentMetaTileDragOrigins.Add(origin))
            return;

        var command = new MetaTilePlaceCommand(
            _page.TileMap,
            metaTile,
            tileX,
            tileY
        );

        if (!command.HasChanges)
            return;

        // ドラッグ中は即時反映して見た目を追従させる。
        // MouseUp時にCompositeCommandとしてUndoStackへ1回分で積む。
        command.Execute();
        _currentMetaTilePlaceCommands.Add(command);

        _mapView.Invalidate();
        _nodeEditorForm?.InvalidatePagePreview(_page.Id, _page);
    }

    private void EndMetaTileDragPlace()
    {
        _isMetaTileDragPlacing = false;

        if (_currentMetaTilePlaceCommands.Count > 0)
        {
            var commands = _currentMetaTilePlaceCommands
                .Cast<ICommand>()
                .ToList();

            _commandManager.Execute(new CompositeCommand(commands));

            _mapView.Invalidate();

            if (_page != null)
                _nodeEditorForm?.InvalidatePagePreview(_page.Id, _page);
        }

        _currentMetaTilePlaceCommands.Clear();
        _currentMetaTileDragOrigins.Clear();
    }

    private void RefreshMapViewBrushPreview()
    {
        if (_context.CurrentBrushKind == EditorBrushKind.MetaTile &&
            _context.SelectedMetaTile != null)
        {
            _mapView.PreviewTileId = -1;
            _mapView.PreviewMetaTile = _context.SelectedMetaTile;
            _mapView.Invalidate();
            return;
        }

        _mapView.PreviewMetaTile = null;
        _mapView.PreviewTileId = _selectedTileId;
        _mapView.Invalidate();
    }

    private void RefreshMetaTilePaletteFromCurrentStage()
    {
        var stage = _context.CurrentStage;

        _metaTilePalette.SetStage(stage);
        _metaTilePalette.SetTileset(_tileset);
        _metaTilePalette.RefreshPalette();

        var selectedMetaTile = _context.SelectedMetaTile;

        if (selectedMetaTile == null || stage == null)
            return;

        var refreshed = stage.FindMetaTile(selectedMetaTile.Id);

        if (refreshed == null)
        {
            _context.SetSelectedMetaTile(null);
            _mapView.PreviewMetaTile = null;
        }
        else
        {
            _context.SetSelectedMetaTile(refreshed);
            _mapView.PreviewMetaTile = refreshed;
        }

        _mapView.Invalidate();
    }
}
