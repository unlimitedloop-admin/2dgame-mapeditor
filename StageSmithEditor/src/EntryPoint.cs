using WinFormsApp = System.Windows.Forms.Application;

namespace StageSmith.Editor;

internal static class EntryPoint
{
    static void Main()
    {
        ApplicationConfiguration.Initialize();
        WinFormsApp.Run(new MainForm
        {
            Icon = new Icon("Assets/SSE-Signature.ico")
        });
    }
}
