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
                //ApplyContextToView();
                //_pageNavBar.UpdateDisplay(_context);
            };

            _nodeEditorForm.Show(this);
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

            _metaTileEditorForm.Show(this);
        }
        else
        {
            _metaTileEditorForm.BringToFront();
        }
    }
}
