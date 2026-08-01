using StageSmith.Editor.Forms;

namespace StageSmith.Editor;

public partial class MainForm
{
    /// <summary>
    /// ノードエディタを開く。既に開いていれば前面に出す。
    /// </summary>
    private void OpenNodeEditor()
    {
        if (_nodeEditorForm == null || _nodeEditorForm.IsDisposed)
        {
            _nodeEditorForm = new PageNodeEditorForm(_context, _commandManager);

            _nodeEditorForm.PageSelected += pageIndex =>
            {
                _context.SetPage(pageIndex);
            };

            _nodeEditorForm.Show();

            // フォーム生成タイミングでは LoadTilesetImage() の対象外だったため、
            // 現在のタイルセット・表示設定を明示的に同期する。
            _nodeEditorForm.SyncTileset(_tileset);
            _nodeEditorForm.SetNumberDisplayFormat(_config.NumberDisplayFormat);
        }
        else
        {
            _nodeEditorForm.BringToFront();
        }
    }

    /// <summary>
    /// メタタイルエディタを開く。既に開いていれば前面に出す。
    /// </summary>
    private void OpenMetaTileEditor()
    {
        var stage = _context.CurrentStage;

        if (stage == null)
        {
            MessageBox.Show(
                this,
                "ステージがありません。",
                "MetaTile Editor",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information
            );
            return;
        }

        if (_metaTileEditorForm == null || _metaTileEditorForm.IsDisposed)
        {
            _metaTileEditorForm = new MetaTileEditorForm(stage, _tileset);

            _metaTileEditorForm.MetaTilesChanged += RefreshMetaTilePaletteFromCurrentStage;

            _metaTileEditorForm.Show();
        }
        else
        {
            _metaTileEditorForm.BringToFront();
        }
    }

    /// <summary>
    /// ステージマップビューアーを開く。既に開いていれば前面に出す。
    /// </summary>
    private void OpenStageMapViewer()
    {
        if (_stageMapViewerForm == null || _stageMapViewerForm.IsDisposed)
        {
            _stageMapViewerForm = new StageMapViewerForm(_context, () => _tileset);
            _stageMapViewerForm.Show();
        }
        else
        {
            _stageMapViewerForm.BringToFront();
        }
    }
    private void OpenTagManager()
    {
        if (_context.Project == null)
        {
            MessageBox.Show(this, "プロジェクトがありません。", "Tag Manager",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        if (_tagManagerForm == null || _tagManagerForm.IsDisposed)
        {
            _tagManagerForm = new TagManagerForm(_context.Project);
            _tagManagerForm.TagsChanged += () =>
            {
                //_nodeEditorForm?.RefreshCurrentPageInfo(); // TODO: Tags:ラベル再描画用（後述）
            };
            _tagManagerForm.Show();
        }
        else
        {
            _tagManagerForm.BringToFront();
        }
    }
}
