using StageSmith.Core.Models;
using StageSmith.Editor.Utilities;

namespace StageSmith.Editor.Controls;

/// <summary>
/// オブジェクトパレット。
///   上段: 画像シートの選択（＋追加 / −削除）とシート全体のグリッド表示（コマ選択）
///   下段: 登録済みの配置用テンプレート一覧（選択＝配置ブラシ）
/// データの追加・削除はこのコントロールでは行わず、イベントで MainForm に委譲する（Undo管理のため）。
/// </summary>
public sealed class ObjectPaletteControl : UserControl
{
    private readonly ToolTip _toolTip = new();
    private readonly ComboBox _sheetCombo;
    private readonly Button _addSheetButton;
    private readonly Button _removeSheetButton;
    private readonly SpriteSheetGridControl _sheetGrid;
    private readonly Button _registerTemplateButton;
    private readonly EntityTemplateListControl _templateList;
    private readonly ComboBox _snapCombo;
    private readonly CheckBox _playerStartToggle;

    private EditorProject? _project;
    private SpriteSheetImageCache? _imageCache;
    private bool _isRefreshing;
    private bool _isReadOnly;

    public event Action? SheetAddRequested;
    public event Action<SpriteSheet>? SheetRemoveRequested;
    public event Action<SpriteSheet, int>? TemplateCreateRequested;
    public event Action<EntityTemplate>? TemplateEditRequested;
    public event Action<EntityTemplate>? TemplateRemoveRequested;
    public event Action<EntityTemplate>? TemplateSelected;
    public event Action<int>? SnapSizeChanged;
    public event Action<bool>? PlayerStartModeChanged;

    public int SnapSize => (_snapCombo.SelectedItem as SnapItem)?.Size ?? 1;

    public SpriteSheet? SelectedSheet => _sheetCombo.SelectedItem as SpriteSheet;

    public ObjectPaletteControl()
    {
        //========================
        // シート選択行
        //========================
        _sheetCombo = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Dock = DockStyle.Fill,
            DisplayMember = nameof(SpriteSheet.Name),
        };

        _addSheetButton = CreateSmallButton("＋", "画像シートを追加...");
        _removeSheetButton = CreateSmallButton("－", "選択中の画像シートを削除");

        // NOTE: Dock の追加順は「後から追加したものほど外側」。Fill のコンボを先に、右端のボタンを後に追加する。
        var sheetRow = new Panel
        {
            Dock = DockStyle.Top,
            Height = 29,
            Padding = new Padding(3, 3, 3, 3),
        };
        sheetRow.Controls.Add(_sheetCombo);
        sheetRow.Controls.Add(_addSheetButton);
        sheetRow.Controls.Add(_removeSheetButton);

        //========================
        // シートグリッド＋登録ボタン
        //========================
        _sheetGrid = new SpriteSheetGridControl { Dock = DockStyle.Fill };

        _registerTemplateButton = new Button
        {
            Text = "選択したコマをテンプレート登録...",
            Dock = DockStyle.Bottom,
            Height = 28,
            Enabled = false,
        };

        //========================
        // テンプレート一覧
        //========================
        _templateList = new EntityTemplateListControl { Dock = DockStyle.Fill };

        _snapCombo = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Dock = DockStyle.Right,
            Width = 64,
        };
        foreach (var size in EntityGeometry.SnapSizes)
            _snapCombo.Items.Add(new SnapItem(size));
        _snapCombo.SelectedIndex = 0;
        _toolTip.SetToolTip(_snapCombo, "配置・移動のスナップ間隔（足元位置）");

        var templateHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = 27,
            Padding = new Padding(4, 2, 3, 2),
        };
        templateHeader.Controls.Add(new Label
        {
            Text = "テンプレート",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
        });
        templateHeader.Controls.Add(new Label
        {
            Text = "スナップ:",
            Dock = DockStyle.Right,
            AutoSize = true,
            Padding = new Padding(0, 4, 2, 0),
        });
        templateHeader.Controls.Add(_snapCombo);

        var split = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Horizontal,
            SplitterWidth = 5,
        };

        split.Panel1.Controls.Add(_sheetGrid);
        split.Panel1.Controls.Add(_registerTemplateButton);
        split.Panel2.Controls.Add(_templateList);
        split.Panel2.Controls.Add(templateHeader);

        //========================
        // プレイヤー開始位置モード（トグル）
        //========================
        _playerStartToggle = new CheckBox
        {
            Text = "プレイヤー開始位置を置く",
            Appearance = Appearance.Button,
            TextAlign = ContentAlignment.MiddleCenter,
            Dock = DockStyle.Bottom,
            Height = 28,
        };
        _toolTip.SetToolTip(_playerStartToggle,
            "ON の間、マップ上の左クリックでプレイヤー開始位置（stage.start）を設定します。\n開始位置マーカーの右クリックで解除します。");

        Controls.Add(split);
        Controls.Add(sheetRow);
        Controls.Add(_playerStartToggle);

        // SplitterDistance はハンドル生成後（サイズ確定後）でないと設定できない
        HandleCreated += (_, _) =>
        {
            if (split.Height > 0)
                split.SplitterDistance = Math.Max(split.Panel1MinSize, (int)(split.Height * 0.55));
        };

        //========================
        // イベント
        //========================
        _sheetCombo.SelectedIndexChanged += (_, _) =>
        {
            if (_isRefreshing) return;
            ApplySelectedSheet();
        };

        _addSheetButton.Click += (_, _) => SheetAddRequested?.Invoke();

        _playerStartToggle.CheckedChanged += (_, _) => PlayerStartModeChanged?.Invoke(_playerStartToggle.Checked);

        _removeSheetButton.Click += (_, _) =>
        {
            if (SelectedSheet is { } sheet)
                SheetRemoveRequested?.Invoke(sheet);
        };

        _sheetGrid.TileSelected += _ => UpdateButtons();
        _sheetGrid.TileActivated += tileIndex =>
        {
            if (SelectedSheet is { } sheet)
                TemplateCreateRequested?.Invoke(sheet, tileIndex);
        };

        _registerTemplateButton.Click += (_, _) =>
        {
            if (SelectedSheet is { } sheet && _sheetGrid.SelectedTileIndex >= 0)
                TemplateCreateRequested?.Invoke(sheet, _sheetGrid.SelectedTileIndex);
        };

        _templateList.TemplateSelected += template =>
        {
            // テンプレートを選んだら、元のシート・コマもグリッド上で示す
            SelectSheet(template.SheetId);
            _sheetGrid.SetSelectedTile(template.TileIndex);
            UpdateButtons();
            TemplateSelected?.Invoke(template);
        };
        _snapCombo.SelectedIndexChanged += (_, _) => SnapSizeChanged?.Invoke(SnapSize);

        _templateList.EditRequested += t => TemplateEditRequested?.Invoke(t);
        _templateList.RemoveRequested += t => TemplateRemoveRequested?.Invoke(t);

        UpdateButtons();
    }

    private Button CreateSmallButton(string text, string toolTip)
    {
        var button = new Button
        {
            Text = text,
            Width = 26,
            Dock = DockStyle.Right,
        };

        _toolTip.SetToolTip(button, toolTip);
        return button;
    }

    //========================
    // 公開API
    //========================

    public void SetProject(EditorProject? project, SpriteSheetImageCache? imageCache)
    {
        _project = project;
        _imageCache = imageCache;
        _templateList.SetProject(project, imageCache);
        RefreshPalette();
    }

    /// <summary>
    /// プロジェクトのシート・テンプレート一覧を読み直す。選択中のシートは Id で維持する。
    /// </summary>
    public void RefreshPalette()
    {
        var selectedId = SelectedSheet?.Id;

        _isRefreshing = true;
        try
        {
            _sheetCombo.BeginUpdate();
            _sheetCombo.Items.Clear();

            foreach (var sheet in _project?.SpriteSheets ?? [])
                _sheetCombo.Items.Add(sheet);

            _sheetCombo.EndUpdate();

            var index = selectedId.HasValue
                ? _project!.SpriteSheets.FindIndex(s => s.Id == selectedId.Value)
                : -1;

            if (index < 0 && _sheetCombo.Items.Count > 0)
                index = 0;

            _sheetCombo.SelectedIndex = index;
        }
        finally
        {
            _isRefreshing = false;
        }

        ApplySelectedSheet();
        _templateList.SetProject(_project, _imageCache);
    }

    public void SelectSheet(Guid sheetId)
    {
        var index = _project?.SpriteSheets.FindIndex(s => s.Id == sheetId) ?? -1;
        if (index >= 0 && _sheetCombo.SelectedIndex != index)
            _sheetCombo.SelectedIndex = index;
    }

    public void SetSelectedTemplate(EntityTemplate? template)
    {
        _templateList.SetSelected(template);
    }

    /// <summary>
    /// スナップ間隔を設定する（候補に無い値なら 1px）。SnapSizeChanged も発火する。
    /// </summary>
    public void SetSnapSize(int size)
    {
        var index = EntityGeometry.SnapSizes.ToList().IndexOf(size);
        _snapCombo.SelectedIndex = Math.Max(0, index);
    }

    /// <summary>プレイヤー開始位置モードの ON/OFF を切り替える（変化した場合は PlayerStartModeChanged を発火）。</summary>
    public void SetPlayerStartMode(bool enabled)
    {
        _playerStartToggle.Checked = enabled;
    }

    public void SetReadOnly(bool isReadOnly)
    {
        _isReadOnly = isReadOnly;
        UpdateButtons();
    }

    //========================
    // 内部
    //========================

    private void ApplySelectedSheet()
    {
        _sheetGrid.SetSheet(SelectedSheet, _imageCache);
        UpdateButtons();
    }

    private sealed record SnapItem(int Size)
    {
        public override string ToString() => $"{Size}px";
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _toolTip.Dispose();

        base.Dispose(disposing);
    }

    private void UpdateButtons()
    {
        _addSheetButton.Enabled = !_isReadOnly && _project != null;
        _removeSheetButton.Enabled = !_isReadOnly && SelectedSheet != null;
        _registerTemplateButton.Enabled = !_isReadOnly && SelectedSheet != null && _sheetGrid.SelectedTileIndex >= 0;
    }
}
