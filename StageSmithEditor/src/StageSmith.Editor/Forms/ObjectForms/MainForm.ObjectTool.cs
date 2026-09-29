using StageSmith.Core.Constants;
using StageSmith.Editor.Tools;
using StageSmith.Editor.Utilities;

namespace StageSmith.Editor;

// オブジェクト（敵などのエンティティ）配置ツール専用partial。
// パレット側（シート・テンプレート管理）は MainForm.ObjectPalette.cs を参照。
public partial class MainForm
{
    private const int NudgeStep = 1;
    private const int NudgeStepLarge = 8;

    private void InitializeObjectTool()
    {
        _objectTool = new ObjectTool(
            () => _context.Project,
            () => _context.CurrentStage,
            () => _page,
            () => _context.SelectedEntityTemplate,
            _commandManager,
            () => _mapView.Invalidate()
        );

        _objectTool.SelectionChanged += OnObjectSelectionChanged;

        _mapView.EntityLayer = new EntityLayerRenderer(
            _spriteSheetImages,
            () => _context.Project,
            () => _context.CurrentStage,
            () => _page,
            () => _context.SelectedEntityTemplate,
            PaletteResolver,
            _objectTool,
            () => _currentMode == EditorToolMode.Object
        );

        _objectPalette.SnapSizeChanged += size =>
        {
            _objectTool.SnapSize = size;
            _mapView.Invalidate();
        };

        // テンプレートを選んだら、そのまま配置できるようにオブジェクトツールへ切り替える
        // （開始位置モード中なら解除してエンティティ配置に戻す）
        _objectPalette.TemplateSelected += _ =>
        {
            _objectPalette.SetPlayerStartMode(false);
            SetToolMode(EditorToolMode.Object);
        };

        _objectPalette.PlayerStartModeChanged += enabled =>
        {
            _objectTool.IsPlacingPlayerStart = enabled;
            if (enabled)
                SetToolMode(EditorToolMode.Object);
        };

        _context.EntityTemplateChanged += () => _mapView.Invalidate();
    }

    /// <summary>
    /// 矢印キーで選択中のエンティティを動かす（Shift 併用で大きく動かす）。処理した場合 true。
    /// </summary>
    private bool TryNudgeSelectedEntity(Keys keyData)
    {
        if (_objectTool?.SelectedEntity == null) return false;

        var step = (keyData & Keys.Shift) != 0 ? NudgeStepLarge : NudgeStep;

        var (dx, dy) = (keyData & ~Keys.Shift) switch
        {
            Keys.Left  => (-step, 0),
            Keys.Right => (step, 0),
            Keys.Up    => (0, -step),
            Keys.Down  => (0, step),
            _          => (0, 0),
        };

        if (dx == 0 && dy == 0) return false;

        _objectTool.Nudge(dx, dy);
        return true;
    }

    private void OnObjectSelectionChanged()
    {
        _propertyWindow.SetSelectedEntity(_objectTool?.SelectedEntity);

        if (_objectTool?.SelectedEntity is not { } entity) return;

        ShowStatusMessage(
            $"{entity.EntityId}  kind={entity.Properties.Kind}  x={entity.X} y={entity.Y}");
    }

    /// <summary>
    /// ページ・ステージ切替時に、前のページのエンティティ選択を残さない。
    /// </summary>
    private void ResetObjectToolForPageChange()
    {
        _objectTool?.ClearSelection();
    }
}
