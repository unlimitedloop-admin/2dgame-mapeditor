using StageSmith.Application.Commands;
using StageSmith.Core.Constants;
using StageSmith.Core.Models;
using StageSmith.Editor.Utilities;

namespace StageSmith.Editor.Tools;

/// <summary>
/// エンティティ（敵など）の配置ツール。部屋内ピクセル座標で操作する。
///   左クリック（空き）          : 選択中テンプレートを配置して選択
///   左クリック（既存）＋ドラッグ : 選択・移動
///   右クリック（既存）          : 削除
/// 位置はスナップ設定（1/8/16px）に従って足元中心を合わせる。
/// </summary>
public sealed class ObjectTool : IPixelTool
{
    private readonly Func<EditorProject?> _getProject;
    private readonly Func<Stage?> _getStage;
    private readonly Func<Page?> _getPage;
    private readonly Func<EntityTemplate?> _getTemplate;
    private readonly CommandManager _commandManager;
    private readonly Action _invalidate;

    private DragState? _drag;

    public ObjectTool(
        Func<EditorProject?> getProject,
        Func<Stage?> getStage,
        Func<Page?> getPage,
        Func<EntityTemplate?> getTemplate,
        CommandManager commandManager,
        Action invalidate)
    {
        _getProject = getProject;
        _getStage = getStage;
        _getPage = getPage;
        _getTemplate = getTemplate;
        _commandManager = commandManager;
        _invalidate = invalidate;
    }

    /// <summary>スナップ間隔（px）。1 ならスナップなし。</summary>
    public int SnapSize { get; set; } = 1;

    public Guid? SelectedEntityId { get; private set; }

    /// <summary>カーソル位置（部屋内ピクセル、未スナップ）。マップ外なら null。</summary>
    public Point? HoverPoint { get; private set; }

    /// <summary>カーソルが既存エンティティの上にあるか（プレビュー・カーソル形状の切替用）。</summary>
    public bool IsHoveringEntity { get; private set; }

    public bool IsDragging => _drag is { Moved: true };

    /// <summary>選択エンティティが変わったとき、または選択中エンティティの位置が確定したとき。</summary>
    public event Action? SelectionChanged;

    public EntityPlacement? SelectedEntity
        => SelectedEntityId is { } id ? _getPage()?.FindEntity(id) : null;

    /// <summary>
    /// true の間は、左クリックでエンティティではなくプレイヤー開始位置を設定する（右クリックで解除）。
    /// </summary>
    public bool IsPlacingPlayerStart
    {
        get => _isPlacingPlayerStart;
        set
        {
            if (_isPlacingPlayerStart == value) return;

            _isPlacingPlayerStart = value;
            _drag = null;
            if (value) Select(null);
            _invalidate();
        }
    }

    private bool _isPlacingPlayerStart;

    /// <summary>配置プレビューを出す位置（スナップ済みの足元）。出さない場合は null。</summary>
    public (int X, int Y)? PreviewFoot
    {
        get
        {
            if (HoverPoint is not { } p || !EntityConstants.IsInsideRoom(p.X, p.Y) || _drag != null)
                return null;

            if (!IsPlacingPlayerStart && (IsHoveringEntity || _getTemplate() == null))
                return null;

            return EntityGeometry.SnapFoot(p.X, p.Y, SnapSize);
        }
    }

    //========================
    // 公開操作
    //========================

    public void Select(EntityPlacement? entity)
    {
        var id = entity?.Id;
        if (SelectedEntityId == id) return;

        SelectedEntityId = id;
        SelectionChanged?.Invoke();
        _invalidate();
    }

    public void ClearSelection()
    {
        _drag = null;
        Select(null);
    }

    /// <summary>選択中のエンティティを削除する。削除した場合 true。</summary>
    public bool DeleteSelected()
    {
        if (_getPage() is not { } page || SelectedEntity is not { } entity) return false;
        if (_commandManager.IsReadOnly) return false;

        _commandManager.Execute(new EntityRemoveCommand(page, entity));
        ClearSelection();
        return true;
    }

    /// <summary>選択中のエンティティを dx, dy ピクセル動かす。動かした場合 true。</summary>
    public bool Nudge(int dx, int dy)
    {
        if (SelectedEntity is not { } entity || _commandManager.IsReadOnly) return false;

        var to = EntityGeometry.ClampToRoom(entity.X + dx, entity.Y + dy);
        var command = new EntityMoveCommand(entity, (entity.X, entity.Y), to);
        if (!command.HasChanges) return true;

        _commandManager.Execute(command);
        SelectionChanged?.Invoke();
        return true;
    }

    //========================
    // IPixelTool
    //========================

    public void OnPixelMouseDown(int px, int py)
    {
        var page = _getPage();
        if (page == null) return;

        if (IsPlacingPlayerStart)
        {
            SetPlayerStartAt(page, px, py);
            return;
        }

        var hit = HitTest(page, px, py);
        if (hit != null)
        {
            Select(hit);

            if (!_commandManager.IsReadOnly)
                _drag = new DragState(hit, (hit.X, hit.Y), hit.X - px, hit.Y - py, px, py);
            return;
        }

        if (!EntityConstants.IsInsideRoom(px, py))
        {
            ClearSelection();
            return;
        }

        var template = _getTemplate();
        var stage = _getStage();

        if (template == null || stage == null || _commandManager.IsReadOnly)
        {
            ClearSelection();
            return;
        }

        var (x, y) = EntityGeometry.SnapFoot(px, py, SnapSize);
        var entity = template.CreatePlacement(stage.GenerateEntityId(template.Properties.Kind), x, y);

        _commandManager.Execute(new EntityPlaceCommand(page, entity));
        Select(entity);
        IsHoveringEntity = true;
    }

    public void OnPixelMouseMove(int px, int py, MouseButtons buttons)
    {
        HoverPoint = new Point(px, py);

        if (_drag is { } drag && (buttons & MouseButtons.Left) != 0)
        {
            // クリックしただけでスナップ位置へ飛ばないよう、ポインタが動いてから移動を始める
            if (!drag.Moved && px == drag.StartX && py == drag.StartY)
                return;

            drag.Moved = true;

            var (x, y) = EntityGeometry.SnapFoot(px + drag.GrabOffsetX, py + drag.GrabOffsetY, SnapSize);
            if (drag.Entity.X != x || drag.Entity.Y != y)
            {
                drag.Entity.X = x;
                drag.Entity.Y = y;
                _invalidate();
            }
            return;
        }

        var page = _getPage();
        IsHoveringEntity = !IsPlacingPlayerStart && page != null && HitTest(page, px, py) != null;
        _invalidate();
    }

    public void OnPixelMouseUp(int px, int py)
    {
        if (_drag is not { } drag) return;
        _drag = null;

        if (!drag.Moved) return;

        // 位置はドラッグ中に反映済み。Undo用に「元位置→現在位置」のコマンドを積む。
        var command = new EntityMoveCommand(drag.Entity, drag.Origin, (drag.Entity.X, drag.Entity.Y));
        if (command.HasChanges)
        {
            _commandManager.Execute(command);
            SelectionChanged?.Invoke();
        }
    }

    public void OnPixelRightMouseDown(int px, int py)
    {
        var page = _getPage();
        if (page == null || _commandManager.IsReadOnly) return;

        if (IsPlacingPlayerStart)
        {
            // 開始位置マーカー上の右クリックで解除
            if (_getStage() is { } stage && HitTestPlayerStart(stage, page, px, py))
                ExecutePlayerStart(stage, null);
            return;
        }

        var hit = HitTest(page, px, py);
        if (hit == null) return;

        _commandManager.Execute(new EntityRemoveCommand(page, hit));

        if (SelectedEntityId == hit.Id)
            ClearSelection();

        IsHoveringEntity = HitTest(page, px, py) != null;
        _invalidate();
    }

    public void OnPixelMouseLeave()
    {
        HoverPoint = null;
        IsHoveringEntity = false;
        _invalidate();
    }

    public Cursor GetCursor(int x, int y)
        => IsHoveringEntity || IsDragging ? Cursors.SizeAll : Cursors.Cross;

    // タイル座標の入力は MapViewControl から呼ばれない（IPixelTool 側が使われる）
    public void OnMouseDown(int x, int y) { }
    public void OnMouseMove(int x, int y) { }
    public void OnMouseUp(int x, int y) { }

    //========================
    // 内部
    //========================

    /// <summary>
    /// 指定位置にあるエンティティを返す。重なっている場合は後から描かれる（リスト後方の）ものを優先する。
    /// </summary>
    public EntityPlacement? HitTest(Page page, int px, int py)
    {
        var project = _getProject();

        for (var i = page.Entities.Count - 1; i >= 0; i--)
        {
            var entity = page.Entities[i];
            if (EntityGeometry.GetRoomBounds(project, entity).Contains(px, py))
                return entity;
        }

        return null;
    }

    private static bool HitTestPlayerStart(Stage stage, Page page, int px, int py)
    {
        return stage.PlayerStart is { } start
            && start.PageId == page.Id
            && EntityGeometry.GetPlayerStartBounds(start.X, start.Y).Contains(px, py);
    }

    private void SetPlayerStartAt(Page page, int px, int py)
    {
        if (_getStage() is not { } stage || _commandManager.IsReadOnly) return;
        if (!EntityConstants.IsInsideRoom(px, py)) return;

        var (x, y) = EntityGeometry.SnapFoot(px, py, SnapSize);
        ExecutePlayerStart(stage, new PlayerStart { PageId = page.Id, X = x, Y = y });
    }

    private void ExecutePlayerStart(Stage stage, PlayerStart? value)
    {
        var command = new PlayerStartSetCommand(stage, value);
        if (!command.HasChanges) return;

        _commandManager.Execute(command);
        _invalidate();
    }

    private sealed class DragState(EntityPlacement entity, (int X, int Y) origin, int grabOffsetX, int grabOffsetY, int startX, int startY)
    {
        public EntityPlacement Entity { get; } = entity;
        public (int X, int Y) Origin { get; } = origin;
        public int GrabOffsetX { get; } = grabOffsetX;
        public int GrabOffsetY { get; } = grabOffsetY;
        public int StartX { get; } = startX;
        public int StartY { get; } = startY;
        public bool Moved { get; set; }
    }
}
