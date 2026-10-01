using StageSmith.Application.Commands;
using StageSmith.Core.Constants;
using StageSmith.Core.Models;
using StageSmith.Editor.Forms;
using StageSmith.Editor.Utilities;
using StageSmith.Infrastructure;

namespace StageSmith.Editor;

// オブジェクトパレット（画像シート・配置用テンプレートの管理）専用partial。
// シート・テンプレートはプロジェクト共有データ（.sseproj）なので、追加・削除・編集はすべて
// ActionCommand 経由で CommandManager に積む（Undo対象かつ未保存判定の対象にするため）。
public partial class MainForm
{
    private const string ObjectPaletteCaption = "Object Palette";

    /// <summary>シート画像のキャッシュ。オブジェクトパレットとマップビューで共有する。</summary>
    private readonly SpriteSheetImageCache _spriteSheetImages = new();

    /// <summary>パレットに反映済みのプロジェクト（プロジェクト切替の検知用）。</summary>
    private EditorProject? _objectPaletteProject;

    private void BindObjectPalette()
    {
        _objectPalette.SheetAddRequested += AddSpriteSheet;
        _objectPalette.SheetRemoveRequested += RemoveSpriteSheet;
        _objectPalette.TemplateCreateRequested += CreateEntityTemplate;
        _objectPalette.TemplateEditRequested += EditEntityTemplate;
        _objectPalette.TemplateRemoveRequested += RemoveEntityTemplate;
        _objectPalette.TemplateSelected += template => _context.SetSelectedEntityTemplate(template);

        _context.EntityTemplateChanged += () => _objectPalette.SetSelectedTemplate(_context.SelectedEntityTemplate);

        FormClosed += (_, _) => _spriteSheetImages.Dispose();
    }

    /// <summary>
    /// プロジェクトが切り替わっていれば、オブジェクトパレットを新しいプロジェクトで作り直す。
    /// ApplyContextToView から毎回呼ばれるため、同一プロジェクトなら何もしない。
    /// </summary>
    private void SyncObjectPaletteProject()
    {
        var project = _context.Project;
        if (ReferenceEquals(project, _objectPaletteProject)) return;

        _objectPaletteProject = project;

        _spriteSheetImages.Clear();
        _context.SetSelectedEntityTemplate(null);

        // シート定義JSONが更新されていれば分割値のキャッシュを追従させる（見つからなければ保存値のまま）
        foreach (var sheet in project?.SpriteSheets ?? [])
            SpriteSheetDefinitionLoader.TryRefresh(sheet);

        _objectPalette.SetProject(project, _spriteSheetImages);
        ReloadEnemyDefinitions();
        _objectPalette.SetReadOnly(_commandManager.IsReadOnly);
    }

    private void RefreshObjectPalette()
    {
        _objectPalette.RefreshPalette();

        // 削除・Undoでテンプレートが消えた場合は選択を解除する
        var selected = _context.SelectedEntityTemplate;
        if (selected != null && _context.Project?.FindEntityTemplate(selected.Id) is null)
            _context.SetSelectedEntityTemplate(null);

        _objectPalette.SetSelectedTemplate(_context.SelectedEntityTemplate);
        _mapView.Invalidate();
    }

    //========================
    // シート
    //========================

    private void AddSpriteSheet()
    {
        if (BlockIfReadOnly(ObjectPaletteCaption)) return;

        var project = _context.Project;
        if (project == null) return;

        using var imageDialog = new OpenFileDialog
        {
            Title = "画像シートを選択",
            Filter = FileExtensions.ImageFilter,
        };

        if (imageDialog.ShowDialog(this) != DialogResult.OK) return;

        var imagePath = imageDialog.FileName;

        var existing = project.SpriteSheets.FirstOrDefault(s =>
            string.Equals(s.ImagePath, imagePath, StringComparison.OrdinalIgnoreCase));

        if (existing != null)
        {
            MessageBox.Show(this, $"この画像シートは登録済みです。\n{existing.Name}", ObjectPaletteCaption,
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            _objectPalette.SelectSheet(existing.Id);
            return;
        }

        // 分割定義は「同名の .json」を探す。自動読み込みが ON で見つかればそのまま使い、
        // OFF または見つからなければ選ばせる（見つかっていれば選択済みの状態で開く）。
        var sameNameDefinition = SpriteSheetDefinitionLoader.FindDefinitionFor(imagePath);
        var definitionPath = _config.AutoLoadSheetDefinition ? sameNameDefinition : null;

        if (definitionPath == null)
        {
            using var definitionDialog = new OpenFileDialog
            {
                Title = "シート分割定義(JSON)を選択",
                Filter = "Sheet Definition (*.json)|*.json|All files (*.*)|*.*",
                InitialDirectory = Path.GetDirectoryName(imagePath),
                FileName = sameNameDefinition != null ? Path.GetFileName(sameNameDefinition) : string.Empty,
            };

            if (definitionDialog.ShowDialog(this) != DialogResult.OK) return;
            definitionPath = definitionDialog.FileName;
        }

        SpriteSheet sheet;
        try
        {
            sheet = SpriteSheetDefinitionLoader.Create(imagePath, definitionPath);
        }
        catch (Exception ex) when (ex is IOException or InvalidDataException or UnauthorizedAccessException)
        {
            MessageBox.Show(this, $"シート分割定義を読み込めませんでした。\n{definitionPath}\n\n{ex.Message}",
                ObjectPaletteCaption, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (!ValidateSpriteSheetImage(sheet)) return;

        _commandManager.Execute(new ActionCommand(
            () =>
            {
                project.SpriteSheets.Add(sheet);
                RefreshObjectPalette();
                _objectPalette.SelectSheet(sheet.Id);
            },
            () =>
            {
                project.SpriteSheets.Remove(sheet);
                RefreshObjectPalette();
            }));
    }

    /// <summary>
    /// 画像が読み込めること、定義上のシートサイズが画像に収まることを確認する。
    /// </summary>
    private bool ValidateSpriteSheetImage(SpriteSheet sheet)
    {
        _spriteSheetImages.Invalidate(sheet.ImagePath);
        var image = _spriteSheetImages.Get(sheet);

        if (image == null)
        {
            MessageBox.Show(this, $"画像を読み込めませんでした。\n{sheet.ImagePath}", ObjectPaletteCaption,
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }

        var requiredWidth = sheet.TileWidth * sheet.TilesX;
        var requiredHeight = sheet.TileHeight * sheet.TilesY;

        if (image.Width < requiredWidth || image.Height < requiredHeight)
        {
            MessageBox.Show(this,
                "画像サイズが分割定義より小さいため登録できません。\n\n" +
                $"定義: {sheet.TileWidth}x{sheet.TileHeight} × {sheet.TilesX}x{sheet.TilesY} = {requiredWidth}x{requiredHeight}\n" +
                $"画像: {image.Width}x{image.Height}",
                ObjectPaletteCaption, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }

        return true;
    }

    private void RemoveSpriteSheet(SpriteSheet sheet)
    {
        if (BlockIfReadOnly(ObjectPaletteCaption)) return;

        var project = _context.Project;
        if (project == null) return;

        var sheetIndex = project.SpriteSheets.IndexOf(sheet);
        if (sheetIndex < 0) return;

        // シートを使っているテンプレートも道連れで削除する（元の位置を覚えてUndoで戻す）
        var removedTemplates = project.EntityTemplates
            .Select((t, i) => (Template: t, Index: i))
            .Where(x => x.Template.SheetId == sheet.Id)
            .ToList();

        var message = removedTemplates.Count == 0
            ? $"画像シート「{sheet.Name}」を削除しますか？"
            : $"画像シート「{sheet.Name}」を削除しますか？\n" +
              $"このシートを使うテンプレート {removedTemplates.Count} 件も削除されます。\n" +
              "（配置済みのオブジェクトは残りますが、画像は仮表示になります）";

        if (MessageBox.Show(this, message, ObjectPaletteCaption, MessageBoxButtons.YesNo, MessageBoxIcon.Warning)
            != DialogResult.Yes)
        {
            return;
        }

        _commandManager.Execute(new ActionCommand(
            () =>
            {
                foreach (var (template, _) in removedTemplates)
                    project.EntityTemplates.Remove(template);

                project.SpriteSheets.Remove(sheet);
                RefreshObjectPalette();
            },
            () =>
            {
                project.SpriteSheets.Insert(Math.Min(sheetIndex, project.SpriteSheets.Count), sheet);

                // 元のインデックス昇順で戻せば、全件が元の位置に並ぶ
                foreach (var (template, index) in removedTemplates)
                    project.EntityTemplates.Insert(Math.Min(index, project.EntityTemplates.Count), template);

                RefreshObjectPalette();
                _objectPalette.SelectSheet(sheet.Id);
            }));
    }

    //========================
    // テンプレート
    //========================

    private IEnumerable<string> CollectKnownEntityKinds()
    {
        var project = _context.Project;
        if (project == null) return [];

        return project.EntityTemplates.Select(t => t.Properties.Kind)
            .Concat(project.Stages.SelectMany(s => s.EnumerateEntities()).Select(x => x.Entity.Properties.Kind));
    }

    private void CreateEntityTemplate(SpriteSheet sheet, int tileIndex)
    {
        if (BlockIfReadOnly(ObjectPaletteCaption)) return;

        var project = _context.Project;
        if (project == null) return;

        using var dialog = new EntityTemplateDialog(sheet, tileIndex, _spriteSheetImages, null, CollectKnownEntityKinds(), PaletteResolver);
        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        var template = new EntityTemplate
        {
            Name       = dialog.TemplateName,
            SheetId    = sheet.Id,
            TileIndex  = tileIndex,
            Type       = dialog.EntityType,
            Properties = dialog.Properties,
        };

        _commandManager.Execute(new ActionCommand(
            () =>
            {
                project.EntityTemplates.Add(template);
                RefreshObjectPalette();
            },
            () =>
            {
                project.EntityTemplates.Remove(template);
                RefreshObjectPalette();
            }));

        // 登録したらそのまま配置ブラシとして選択する
        _context.SetSelectedEntityTemplate(template);
    }

    private void EditEntityTemplate(EntityTemplate template)
    {
        if (BlockIfReadOnly(ObjectPaletteCaption)) return;

        var project = _context.Project;
        if (project == null) return;

        var sheet = project.FindSpriteSheet(template.SheetId);
        if (sheet == null)
        {
            MessageBox.Show(this, "このテンプレートの画像シートが見つかりません。", ObjectPaletteCaption,
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        using var dialog = new EntityTemplateDialog(sheet, template.TileIndex, _spriteSheetImages, template, CollectKnownEntityKinds(), PaletteResolver);
        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        var oldValues = (template.Name, template.Type, Properties: template.Properties.Clone());
        var newValues = (Name: dialog.TemplateName, Type: dialog.EntityType, Properties: dialog.Properties);

        _commandManager.Execute(new ActionCommand(
            () =>
            {
                template.Name = newValues.Name;
                template.Type = newValues.Type;
                template.Properties = newValues.Properties.Clone();
                RefreshObjectPalette();
            },
            () =>
            {
                template.Name = oldValues.Name;
                template.Type = oldValues.Type;
                template.Properties = oldValues.Properties.Clone();
                RefreshObjectPalette();
            }));
    }

    private void RemoveEntityTemplate(EntityTemplate template)
    {
        if (BlockIfReadOnly(ObjectPaletteCaption)) return;

        var project = _context.Project;
        if (project == null) return;

        var index = project.EntityTemplates.IndexOf(template);
        if (index < 0) return;

        var usedCount = project.Stages
            .SelectMany(s => s.EnumerateEntities())
            .Count(x => x.Entity.TemplateId == template.Id);

        var message = usedCount == 0
            ? $"テンプレート「{template.Name}」を削除しますか？"
            : $"テンプレート「{template.Name}」を削除しますか？\n" +
              $"配置済みの {usedCount} 体は残りますが、画像は仮表示になります。";

        if (MessageBox.Show(this, message, ObjectPaletteCaption, MessageBoxButtons.YesNo, MessageBoxIcon.Warning)
            != DialogResult.Yes)
        {
            return;
        }

        _commandManager.Execute(new ActionCommand(
            () =>
            {
                project.EntityTemplates.Remove(template);
                RefreshObjectPalette();
            },
            () =>
            {
                project.EntityTemplates.Insert(Math.Min(index, project.EntityTemplates.Count), template);
                RefreshObjectPalette();
            }));
    }
}
