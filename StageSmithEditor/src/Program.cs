namespace StageSmith.Editor;

internal static class Program
{
    static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm
        {
            Icon = new Icon("Assets/SSE-Signature.ico")
        });
    }
}
