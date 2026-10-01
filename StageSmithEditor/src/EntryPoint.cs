using StageSmith.Editor.Utilities;
using WinFormsApp = System.Windows.Forms.Application;

namespace StageSmith.Editor;

internal static class EntryPoint
{
    private const string IconPath = "Assets/SSE-Signature.ico";

    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();

        var mainForm = new MainForm();

        // 作業フォルダーに関係なく exe の場所から読む。見つからなくても起動は続ける（既定のアイコンになる）
        var iconPath = AppPaths.Resolve(IconPath);
        if (File.Exists(iconPath))
            mainForm.Icon = new Icon(iconPath);

        WinFormsApp.Run(mainForm);
    }
}
