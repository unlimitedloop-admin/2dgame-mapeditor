using System.ComponentModel;
using StageSmith.Application.Commands;
using StageSmith.Core.Constants;
using StageSmith.Core.Models;
using StageSmith.Editor.Utilities;

namespace StageSmith.Editor.Controls;

// PropertyWindowControl のエンティティ配置関連partial。
//   - Stage  : Player Start（.def の stage.start）の表示とクリア
//   - Page   : Enemy Respawn（.def の nodes[].enemyRespawn）
//   - Entity : オブジェクトツールで選択中のエンティティ（.def の entities[] の1件）
public sealed partial class PropertyWindowControl
{
    private const string EntityCaption = "Entity";

    private Label _playerStartLabel = null!;
    private Button _playerStartClearButton = null!;
    private ComboBox _pageEnemyRespawnCombo = null!;

    private TextBox _entityIdTextBox = null!;
    private TextBox _entityTypeTextBox = null!;
    private ComboBox _entityKindCombo = null!;
    private NumericUpDown _entityXNumeric = null!;
    private NumericUpDown _entityYNumeric = null!;
    private ComboBox _entityPaletteCombo = null!;
    private ComboBox _entityFacingCombo = null!;
    private ComboBox _entityRespawnCombo = null!;
    private ComboBox _entityDespawnCombo = null!;

    private Control[] _entityEditors = [];

    /// <summary>プロパティ表示中のエンティティ（オブジェクトツールの選択）。</summary>
    private EntityPlacement? _entity;

    /// <summary>
    /// 読み込み済みの敵定義（kind の候補・palette プリセットの選択肢に使う）。未設定なら自由入力のみ。
    /// </summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Func<EnemyDefinitionCatalog?>? EnemyDefinitions { get; set; }

    private EnemyDefinitionCatalog Catalog => EnemyDefinitions?.Invoke() ?? EnemyDefinitionCatalog.Empty;

    /// <summary>
    /// オブジェクトツールの選択を反映する。
    /// 切り替える前に、表示中エンティティへの未確定の編集を確定させる
    /// （フォーカスアウトより先に選択が変わった場合に、編集が別のエンティティへ書き込まれるのを防ぐ）。
    /// </summary>
    public void SetSelectedEntity(EntityPlacement? entity)
    {
        if (ReferenceEquals(_entity, entity))
        {
            RefreshEntitySection();
            return;
        }

        CommitEntityEdits();
        _entity = entity;
        RefreshEntitySection();
    }

    //========================
    // 構築
    //========================

    private Label AddPlayerStartRow(string labelText)
    {
        var row = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            AutoSize = true,
        };

        var valueLabel = new Label
        {
            AutoSize = true,
            Padding = new Padding(0, 6, 6, 0),
            Text = "-",
        };

        _playerStartClearButton = new Button
        {
            Text = "クリア",
            AutoSize = true,
        };
        _playerStartClearButton.Click += (_, _) => ClearPlayerStart();

        row.Controls.Add(valueLabel);
        row.Controls.Add(_playerStartClearButton);

        AddRow(CreateLabel(labelText), row);
        return valueLabel;
    }

    private ComboBox AddChoiceRow(string labelText, Action<ComboBox> fill)
    {
        var combo = new ComboBox
        {
            Dock = DockStyle.Fill,
            DropDownStyle = ComboBoxStyle.DropDownList,
        };
        fill(combo);
        combo.SelectedIndex = 0;

        AddRow(CreateLabel(labelText), combo);
        return combo;
    }

    /// <summary>候補から選べるが自由入力もできるコンボ（kind / palette 用）。</summary>
    private ComboBox AddEditableComboRow(string labelText)
    {
        var combo = new ComboBox
        {
            Dock = DockStyle.Fill,
            DropDownStyle = ComboBoxStyle.DropDown,
        };

        AddRow(CreateLabel(labelText), combo);
        return combo;
    }

    private TextBox AddEntityTextRow(string labelText, string? placeholder = null, bool readOnly = false)
    {
        var textBox = AddTextRow(labelText);
        textBox.ReadOnly = readOnly;
        if (placeholder != null) textBox.PlaceholderText = placeholder;
        return textBox;
    }

    private NumericUpDown AddPixelRow(string labelText, int maximum)
    {
        // エンティティ座標は部屋内ピクセルなので、Hex表示設定に関わらず10進で扱う
        var numeric = new NumericUpDown
        {
            Dock = DockStyle.Fill,
            Minimum = 0,
            Maximum = maximum,
        };

        AddRow(CreateLabel(labelText), numeric);
        EnableValidatedPaste(numeric);
        return numeric;
    }

    private void InitializeEntitySection()
    {
        AddHeader(EntityCaption, Color.FromArgb(170, 215, 255));

        _entityIdTextBox = AddEntityTextRow("ID");
        _entityTypeTextBox = AddEntityTextRow("Type", readOnly: true);
        _entityKindCombo = AddEditableComboRow("Kind");
        _entityXNumeric = AddPixelRow("X", EntityConstants.RoomPixelWidth - 1);
        _entityYNumeric = AddPixelRow("Y", EntityConstants.RoomPixelHeight - 1);
        _entityPaletteCombo = AddEditableComboRow("Palette");
        _entityFacingCombo = AddChoiceRow("Facing", ChoiceItem.FillFacing);
        _entityRespawnCombo = AddChoiceRow("Respawn", c => ChoiceItem.FillRespawn(c));
        _entityDespawnCombo = AddChoiceRow("Despawn", ChoiceItem.FillDespawn); // despawnOffscreen（ラベル列の幅に収めるため短縮）

        _entityEditors =
        [
            _entityIdTextBox, _entityTypeTextBox, _entityKindCombo,
            _entityXNumeric, _entityYNumeric, _entityPaletteCombo,
            _entityFacingCombo, _entityRespawnCombo, _entityDespawnCombo,
        ];

        // テキスト・数値はフォーカスアウトで、コンボは選択確定で1操作として確定する
        foreach (var control in _entityEditors)
            control.Leave += (_, _) => CommitEntityEdits();

        foreach (var combo in new[] { _entityFacingCombo, _entityRespawnCombo, _entityDespawnCombo })
            combo.SelectionChangeCommitted += (_, _) => CommitEntityEdits();

        // 自由入力可のコンボは SelectionChangeCommitted の時点で Text がまだ古いので、処理の後に確定する
        foreach (var combo in new[] { _entityKindCombo, _entityPaletteCombo })
            combo.SelectionChangeCommitted += (_, _) =>
            {
                if (IsHandleCreated) BeginInvoke(CommitEntityEdits);
                else CommitEntityEdits();
            };

        // kind を変えたら、palette の選択肢をその kind のプリセットに入れ替える
        _entityKindCombo.TextChanged += (_, _) =>
        {
            if (_isRefreshing) return;
            ChoiceItem.FillPalette(_entityPaletteCombo, Catalog, _entityKindCombo.Text.Trim());
        };

        _pageEnemyRespawnCombo.SelectionChangeCommitted += (_, _) => CommitPageEnemyRespawn();

        RefreshEntitySection();
    }

    //========================
    // 表示更新
    //========================

    private void RefreshPlayerStart()
    {
        var stage = _context?.CurrentStage;
        var start = stage?.PlayerStart;

        if (stage == null || start == null)
        {
            _playerStartLabel.Text = "-";
            _playerStartClearButton.Enabled = false;
            return;
        }

        var page = stage.FindPage(start.PageId);
        _playerStartLabel.Text = page == null
            ? $"(ページなし) ({start.X}, {start.Y})"
            : $"{page.Name} ({start.X}, {start.Y})";
        _playerStartClearButton.Enabled = true;
    }

    private void RefreshEntitySection()
    {
        // Undoで配置が取り消された等、表示中のエンティティがページから消えていたら表示をやめる
        if (_entity != null && _context?.CurrentPage?.Entities.Contains(_entity) != true)
            _entity = null;

        var wasRefreshing = _isRefreshing;
        _isRefreshing = true;

        try
        {
            var entity = _entity;
            var enabled = entity != null;

            foreach (var control in _entityEditors)
                control.Enabled = enabled;

            _entityIdTextBox.Text = entity?.EntityId ?? "";
            _entityTypeTextBox.Text = entity?.Type ?? "";
            var catalog = Catalog;
            var kind = entity?.Properties.Kind ?? "";

            _entityKindCombo.BeginUpdate();
            _entityKindCombo.Items.Clear();
            foreach (var definition in catalog.Definitions)
                _entityKindCombo.Items.Add(definition.Id);
            _entityKindCombo.EndUpdate();
            _entityKindCombo.Text = kind;

            _entityXNumeric.Value = Math.Clamp(entity?.X ?? 0, 0, (int)_entityXNumeric.Maximum);
            _entityYNumeric.Value = Math.Clamp(entity?.Y ?? 0, 0, (int)_entityYNumeric.Maximum);

            ChoiceItem.FillPalette(_entityPaletteCombo, catalog, kind);
            ChoiceItem.SetEditableValue(_entityPaletteCombo, entity?.Properties.Palette);
            ChoiceItem.Select(_entityFacingCombo, entity?.Properties.Facing);
            ChoiceItem.Select(_entityRespawnCombo, entity?.Properties.Respawn);
            ChoiceItem.Select(_entityDespawnCombo, ChoiceItem.FromBool(entity?.Properties.DespawnOffscreen));
        }
        finally
        {
            _isRefreshing = wasRefreshing;
        }
    }

    //========================
    // 編集の確定
    //========================

    private void CommitEntityEdits()
    {
        if (_isRefreshing || _entity == null) return;

        var entity = _entity;
        var before = EntitySnapshot.Capture(entity);
        var after = new EntitySnapshot(
            _entityIdTextBox.Text.Trim(),
            (int)_entityXNumeric.Value,
            (int)_entityYNumeric.Value,
            new EntityProperties
            {
                Kind             = _entityKindCombo.Text.Trim(),
                Palette          = ChoiceItem.GetEditableValue(_entityPaletteCombo),
                Facing           = ChoiceItem.GetValue(_entityFacingCombo),
                Respawn          = ChoiceItem.GetValue(_entityRespawnCombo),
                DespawnOffscreen = ChoiceItem.ToBool(ChoiceItem.GetValue(_entityDespawnCombo)),
            });

        if (before.SameAs(after)) return;

        var error = ValidateEntityEdit(entity, after);
        if (error != null)
        {
            MessageBox.Show(FindForm(), error, EntityCaption, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            RefreshEntitySection();
            return;
        }

        CommandRequested?.Invoke(new ActionCommand(
            () => after.ApplyTo(entity),
            () => before.ApplyTo(entity)));
    }

    /// <summary>
    /// .def 出力時にエラーになる入力（id 空・重複、kind 空）をその場で弾く。
    /// </summary>
    private string? ValidateEntityEdit(EntityPlacement entity, EntitySnapshot after)
    {
        if (string.IsNullOrEmpty(after.EntityId))
            return "ID を入力してください。";

        if (string.IsNullOrEmpty(after.Properties.Kind))
            return "Kind（敵定義の id）を入力してください。";

        var duplicated = _context?.CurrentStage?.EnumerateEntities()
            .Any(x => !ReferenceEquals(x.Entity, entity) && x.Entity.EntityId == after.EntityId) == true;

        return duplicated
            ? $"ID「{after.EntityId}」はこのステージ内で既に使われています。"
            : null;
    }

    private void CommitPageEnemyRespawn()
    {
        if (_isRefreshing) return;

        var page = _context?.CurrentPage;
        if (page == null) return;

        var oldValue = page.EnemyRespawn;
        var newValue = ChoiceItem.GetValue(_pageEnemyRespawnCombo);
        if (oldValue == newValue) return;

        CommandRequested?.Invoke(new ActionCommand(
            () => page.EnemyRespawn = newValue,
            () => page.EnemyRespawn = oldValue));
    }

    private void ClearPlayerStart()
    {
        var stage = _context?.CurrentStage;
        if (stage?.PlayerStart == null) return;

        CommandRequested?.Invoke(new PlayerStartSetCommand(stage, null));
    }

    /// <summary>エンティティの編集対象値のスナップショット（Undo/Redo用）。</summary>
    private sealed record EntitySnapshot(string EntityId, int X, int Y, EntityProperties Properties)
    {
        public static EntitySnapshot Capture(EntityPlacement e)
            => new(e.EntityId, e.X, e.Y, e.Properties.Clone());

        public void ApplyTo(EntityPlacement e)
        {
            e.EntityId = EntityId;
            e.X = X;
            e.Y = Y;
            e.Properties = Properties.Clone();
        }

        public bool SameAs(EntitySnapshot other)
            => EntityId == other.EntityId
            && X == other.X
            && Y == other.Y
            && Properties.Kind == other.Properties.Kind
            && Properties.Palette == other.Properties.Palette
            && Properties.Facing == other.Properties.Facing
            && Properties.Respawn == other.Properties.Respawn
            && Properties.DespawnOffscreen == other.Properties.DespawnOffscreen;
    }
}
