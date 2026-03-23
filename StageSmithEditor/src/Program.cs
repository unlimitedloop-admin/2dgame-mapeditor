using StageSmith.Core.Models;
using StageSmithEditor.src;

namespace StageSmith.Editor;

internal static class Program
{
    static void Main()
    {
        ApplicationConfiguration.Initialize();

        // ここに書くよ
        RunTest();

        Application.Run(new MainForm());
    }

    static void RunTest()
    {
        var project = new EditorProject
        {
            Name = "Test Project",
            TilesetImagePath = @"Assets\DEMOSTAGE2_N0_ALL_PATTERN.png"
        };

        var stage = project.AddStage("Stage 1");
        var page = stage.AddPage("Start Page");

        page.TileMap.SetTile(0, 0, 5);

        var json = System.Text.Json.JsonSerializer.Serialize(
            project,
            new System.Text.Json.JsonSerializerOptions { WriteIndented = true }
        );

        File.WriteAllText("test_project.def", json);
    }
}