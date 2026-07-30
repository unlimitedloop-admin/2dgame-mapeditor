using StageSmith.Editor.Forms;

namespace StageSmith.Editor;

public partial class MainForm
{
    private void OpenEditorPropertiesDialog()
    {
        using var dialog = new EditorPropertiesDialog(_config);

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            _config = dialog.Result;

            // NOTE: 各設定項目の実際の適用（Hex/Decimal表示切替の反映箇所、
            // タイルクリア時の挙動、ステージ保存構造の変更等）は、
            // 項目ごとに影響範囲を確認しながら別途実装する。
            // ここでは EditorConfig への確定のみを行う（次回起動時にも ini 経由で反映される）。
        }
    }
}
