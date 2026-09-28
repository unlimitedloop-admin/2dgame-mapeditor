using StageSmith.Core.Constants;
using StageSmith.Editor.Tools;
using StageSmith.Editor.Utilities;

namespace StageSmith.Editor.Controls;

// MapViewControl の入力処理専用partial（マウス操作・座標変換・カーソル制御）。
// 分割方針は MapViewControl.cs 参照。
public partial class MapViewControl
{
    public event EventHandler<TileContextMenuEventArgs>? ContextMenuRequested;

    private void OnToolChanged(ITool? tool)
    {
        Cursor = Cursors.Default;
        Invalidate();
    }

    // =========================
    // 入力
    // =========================
    protected override void OnMouseDown(MouseEventArgs e)
    {
        // NOTE: コンテキストメニュー要求はMouseUp側で発火するため、MouseDown側では処理しない。

        base.OnMouseDown(e);

        if (_tileMap == null) return;

        if (e.Button == MouseButtons.Left &&
            TryHitAdjacentNavigationButton(e.Location, out var direction))
        {
            var hasAdjacent = _adjacentState.HasAdjacent(direction);
            AdjacentNavigationRequested?.Invoke(
                this,
                new AdjacentNavigationRequestedEventArgs(direction, hasAdjacent)
            );
            return;
        }

        // ピクセル座標ツール（オブジェクト配置）は、タイル用の Alt/Shift 操作や MetaTile 抑止を通さず直接渡す
        if (_toolManager?.CurrentTool is IPixelTool pixelTool)
        {
            var (px, py) = ScreenToRoomPixel(e.X, e.Y);

            if (e.Button == MouseButtons.Left)
                pixelTool.OnPixelMouseDown(px, py);
            else if (e.Button == MouseButtons.Right)
                pixelTool.OnPixelRightMouseDown(px, py);

            Cursor = pixelTool.GetCursor(px, py);
            Invalidate();
            return;
        }

        var (x, y) = ScreenToTile(e.X, e.Y);
        if (!IsInside(x, y)) return;

        if (e.Button == MouseButtons.Right)
        {
            if (ReferenceEquals(_toolManager?.CurrentTool, MarkerTool))
            {
                MarkerTool?.OnRightMouseDown(x, y);
            }
            return;
        }

        var isAlt = (ModifierKeys & Keys.Alt) != 0;

        // Alt+左クリックのスポイトは、MetaTileブラシ中でも常に機能させる。
        // （ShouldSuppressToolInputより前に判定することで迂回させる）
        if (e.Button == MouseButtons.Left && isAlt)
        {
            PickerTool?.OnMouseDown(x, y);
            return;
        }

        // MetaTileなど、外部側で左クリックを処理する特殊ブラシの場合、
        // MapViewControl内部のTool処理へ入力を渡さない（Alt+クリックは上で処理済みなのでここには来ない）。
        if (e.Button == MouseButtons.Left &&
            ShouldSuppressToolInput?.Invoke() == true)
        {
            return;
        }

        var isShift = (ModifierKeys & Keys.Shift) != 0;

        if (isShift)
        {
            // Selectionツール使用中は Shift+クリック＝選択範囲の対角拡張
            if (ReferenceEquals(_toolManager?.CurrentTool, SelectionTool))
            {
                SelectionTool?.ExtendSelection(x, y);
                Invalidate();
                return;
            }

            // それ以外（Penツールなど）は従来通りフィル
            FillTool?.OnMouseDown(x, y);
            Invalidate();
            return;
        }

        _toolManager?.CurrentTool?.OnMouseDown(x, y);
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);

        if (_tileMap == null) return;

        // ピクセル座標ツール使用中は右クリック＝削除のため、コンテキストメニューも出さない
        if (_toolManager?.CurrentTool is IPixelTool pixelTool)
        {
            if (e.Button == MouseButtons.Left)
            {
                var (px, py) = ScreenToRoomPixel(e.X, e.Y);
                pixelTool.OnPixelMouseUp(px, py);
                Invalidate();
            }
            return;
        }

        var (x, y) = ScreenToTile(e.X, e.Y);

        // ========================
        // 右クリック：コンテキストメニュー要求を発火
        // ========================
        if (e.Button == MouseButtons.Right)
        {
            if (ReferenceEquals(_toolManager?.CurrentTool, MarkerTool))
            {
                return; // 削除はMouseDown側で処理済み。コンテキストメニューは抑止する。
            }

            if (IsInside(x, y))
            {
                ContextMenuRequested?.Invoke(
                    this,
                    new TileContextMenuEventArgs(x, y, PointToScreen(e.Location))
                );
            }
            return;
        }

        // NOTE: 範囲外ドロップを許可するため、ここではIsInsideチェックしない


        if (e.Button == MouseButtons.Left &&
            ShouldSuppressToolInput?.Invoke() == true)
        {
            return;
        }

        var isAlt = (ModifierKeys & Keys.Alt) != 0;

        if (isAlt)
        {
            PickerTool?.OnMouseUp(x, y);
            return;
        }

        _toolManager?.CurrentTool?.OnMouseUp(x, y);
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);

        if (_tileMap == null) return;

        if (_toolManager?.CurrentTool is IPixelTool pixelTool)
        {
            var (px, py) = ScreenToRoomPixel(e.X, e.Y);
            pixelTool.OnPixelMouseMove(px, py, e.Button);

            var (tx, ty) = ScreenToTile(e.X, e.Y);
            _hoverTile = IsInside(tx, ty) ? new Point(tx, ty) : new Point(-1, -1);
            Cursor = IsInside(tx, ty) ? pixelTool.GetCursor(px, py) : Cursors.Default;
            Invalidate();
            return;
        }

        var (x, y) = ScreenToTile(e.X, e.Y);

        if (!IsInside(x, y))
        {
            Cursor = Cursors.Default;
            _hoverTile = new Point(-1, -1);
            _tileInfoToolTip.Hide(this);
            Invalidate();
            return;
        }

        var newHover = new Point(x, y);
        if (_hoverTile != newHover)
        {
            _hoverTile = newHover;
            UpdateTileInfoToolTip(e.Location);
            Invalidate();
        }

        var currentTool = _toolManager?.CurrentTool;
        if (e.Button == MouseButtons.Left)
        {
            if (ShouldSuppressToolInput?.Invoke() != true)
            {
                currentTool?.OnMouseMove(x, y);
            }
        }
        Invalidate();
    }

    // =========================
    // 補助
    // =========================
    private (int x, int y) ScreenToTile(int px, int py)
    {
        var dstSize = CurrentTileRenderSize;

        return (
            FloorDiv(px - OffsetX, dstSize),
            FloorDiv(py - OffsetY, dstSize)
        );
    }

    /// <summary>
    /// 負数を正しく負の無限大方向へ切り捨てる整数除算。
    /// C#標準の `/` 演算子は0方向へ切り捨てるため、
    /// マイナス側の座標（マージン領域クリック時など）で誤った結果になるのを防ぐ。
    /// </summary>
    private static int FloorDiv(int a, int b)
    {
        var q = a / b;
        if (a % b != 0 && (a < 0) != (b < 0))
            q--;
        return q;
    }

    /// <summary>
    /// 画面座標を部屋内ピクセル座標（0〜255, 0〜239 が部屋の範囲）へ変換する。範囲外もそのまま返す。
    /// </summary>
    private (int x, int y) ScreenToRoomPixel(int px, int py)
    {
        var dstSize = CurrentTileRenderSize;
        var srcSize = MapConstants.DefaultTileSize;

        return (
            FloorDiv((px - OffsetX) * srcSize, dstSize),
            FloorDiv((py - OffsetY) * srcSize, dstSize)
        );
    }

    public bool TryScreenToTile(int px, int py, out int x, out int y)
    {
        (x, y) = ScreenToTile(px, py);
        return IsInside(x, y);
    }

    private void UpdateTileInfoToolTip(Point clientLocation)
    {
        if (!_showTileInfo || _tileMap == null)
        {
            _tileInfoToolTip.Hide(this);
            return;
        }

        var tileId = _tileMap.GetTile(_hoverTile.X, _hoverTile.Y);
        var text = $"Tile: {NumberFormatHelper.FormatByte(tileId, _numberDisplayFormat)}\n" +
                   $"(x: {NumberFormatHelper.FormatColumnIndex(_hoverTile.X, _numberDisplayFormat)}, " +
                   $"y: {NumberFormatHelper.FormatRowIndex(_hoverTile.Y, _numberDisplayFormat)})";

        // カーソルに重ならないよう少し右下へオフセットして表示
        _tileInfoToolTip.Show(text, this, clientLocation.X + 16, clientLocation.Y + 16, 3000);
    }

    private bool IsInside(int x, int y)
    {
        return _tileMap != null &&
               x >= 0 && x < _tileMap.Width &&
               y >= 0 && y < _tileMap.Height;
    }

    public void UpdateCursor()
    {
        var isShift = (ModifierKeys & Keys.Shift) != 0;
        var isAlt = (ModifierKeys & Keys.Alt) != 0;

        var (x, y) = _hoverTile.X >= 0 ? (_hoverTile.X, _hoverTile.Y) : (-1, -1);

        if (x < 0 || y < 0)
        {
            Cursor = Cursors.Default;
            return;
        }

        var currentTool = _toolManager?.CurrentTool;

        // ピクセル座標ツールは Alt/Shift のタイル用操作を持たないので、ツール自身のカーソルのみ
        if (currentTool is IPixelTool)
        {
            Cursor = currentTool.GetCursor(x, y);
            return;
        }

        // スポイト（Alt併用、右クリックの有無に関わらずカーソルで予告）
        if (isAlt)
        {
            Cursor = PickerTool?.GetCursor(x, y) ?? Cursors.Hand;
            return;
        }

        // バケツ（Pen限定）
        if (isShift && currentTool is PenTool)
        {
            Cursor = FillTool?.GetCursor(x, y) ?? Cursors.Hand;
            return;
        }

        // Bucketツール単体選択時は、ツールバー/メニューのチェック状態で判別できるため
        // 通常ポインタのまま（Pen/Selectionと同じ流儀）にする
        if (currentTool is FillTool)
        {
            Cursor = Cursors.Default;
            return;
        }

        Cursor = currentTool?.GetCursor(x, y) ?? Cursors.Default;
    }
}
