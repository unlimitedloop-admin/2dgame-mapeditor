using StageSmith.Application.Commands;
using StageSmith.Core.Models;
using StageSmith.Editor.Forms;
using StageSmith.Editor.Utilities;
using StageSmith.Infrastructure;

namespace StageSmith.Editor;

// 敵定義（ゲーム側 assets/data/enemies/*.json）と NES パレットの読み込み専用partial。
// palette プリセットの選択肢・色替え表示・def 出力前の kind / palette 検証に使う。
// 読み込み結果は実行時のみ保持し、保存するのは2つのパス（EditorProject）だけ。
public partial class MainForm
{
    private const string EnemyDefinitionCaption = "敵定義・パレットの設定";

    private EnemyDefinitionCatalog _enemyDefinitions = EnemyDefinitionCatalog.Empty;
    private EntityPaletteResolver? _paletteResolver;

    /// <summary>描画・UI で共有する palette リゾルバ（常に最新の _enemyDefinitions を参照する）。</summary>
    private EntityPaletteResolver PaletteResolver => _paletteResolver ??= new EntityPaletteResolver(() => _enemyDefinitions);

    private void BindEnemyDefinitions()
    {
        _objectPalette.PaletteResolver = PaletteResolver;
        _objectPalette.EnemyDefinitionSettingsRequested += OpenEnemyDefinitionSettings;
        _propertyWindow.EnemyDefinitions = () => _enemyDefinitions;
    }

    /// <summary>
    /// 現在のプロジェクトの設定で敵定義・NES パレットを読み直し、表示に反映する。
    /// </summary>
    private void ReloadEnemyDefinitions()
    {
        var project = _context.Project;

        _enemyDefinitions = project == null ||
            (string.IsNullOrWhiteSpace(project.EnemyDefinitionDirectory) && string.IsNullOrWhiteSpace(project.NesPalettePath))
                ? EnemyDefinitionCatalog.Empty
                : EnemyDefinitionCatalogLoader.Load(project.EnemyDefinitionDirectory, project.NesPalettePath);

        // 色替え結果は敵定義に依存するので作り直す
        _spriteSheetImages.ClearRecolored();

        _objectPalette.RefreshPalette();
        _propertyWindow.RefreshProperties();
        _mapView.Invalidate();
    }

    private void OpenEnemyDefinitionSettings()
    {
        var project = _context.Project;
        if (project == null) return;

        using var dialog = new EnemyDefinitionSettingsDialog(project.EnemyDefinitionDirectory, project.NesPalettePath);
        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        var oldValues = (project.EnemyDefinitionDirectory, project.NesPalettePath);
        var newValues = (dialog.EnemyDefinitionDirectory, dialog.NesPalettePath);

        // 変更が無ければ「再読込」として扱う（ゲーム側の JSON を編集した後に使う）
        if (oldValues == newValues || BlockIfReadOnly(EnemyDefinitionCaption))
        {
            ReloadEnemyDefinitions();
            ShowEnemyDefinitionLoadResult();
            return;
        }

        _commandManager.Execute(new ActionCommand(
            () =>
            {
                (project.EnemyDefinitionDirectory, project.NesPalettePath) = newValues;
                ReloadEnemyDefinitions();
            },
            () =>
            {
                (project.EnemyDefinitionDirectory, project.NesPalettePath) = oldValues;
                ReloadEnemyDefinitions();
            }));

        ShowEnemyDefinitionLoadResult();
    }

    private void ShowEnemyDefinitionLoadResult()
    {
        var summary = _enemyDefinitions.HasDefinitions
            ? $"敵定義 {_enemyDefinitions.Definitions.Count()} 件を読み込みました"
            : "敵定義は読み込まれていません";

        ShowStatusMessage(_enemyDefinitions.Errors.Count == 0
            ? summary
            : $"{summary}（問題 {_enemyDefinitions.Errors.Count} 件。⚙ から詳細を確認できます）");
    }
}
