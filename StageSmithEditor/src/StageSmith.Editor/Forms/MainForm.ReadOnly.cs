namespace StageSmith.Editor;

public partial class MainForm
{
    /// <summary>
    /// 読み取り専用モードの適用。CommandManagerをロックし、タイトルバーへ反映する。
    /// </summary>
    private void ApplyReadOnlyState(bool isReadOnly)
    {
        _commandManager.IsReadOnly = isReadOnly;
        UpdateTitle();

        _metaTileEditorForm?.SetReadOnly(isReadOnly);
        _tagManagerForm?.SetReadOnly(isReadOnly);
        _stageExplorer.IsReadOnly = isReadOnly;

        UpdateEditorAvailability(); // ツールバーのボタンの有効/無効を更新
    }

    private bool BlockIfReadOnly(string commandName)
    {
        if (!_commandManager.IsReadOnly) return false;

        MessageBox.Show(
            this,
            "読み取り専用モードのため、操作できません。",
            commandName,
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);

        return true;
    }
}
