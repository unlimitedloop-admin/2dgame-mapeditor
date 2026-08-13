using StageSmith.Core.Constants;
using StageSmith.Core.Models;

namespace StageSmith.Application.Services;

/// <summary>
/// プロジェクトの新規作成を担当するファクトリクラス
/// </summary>
public static class ProjectFactory
{
    public static EditorProject CreateNewProject()
    {
        var project = new EditorProject
        {
            Name = "New Project"
        };

        var stage = project.AddStage("Stage 001");
        stage.StageNumber = 0;
        stage.Key = "stage_001";

        var page = stage.AddPage("Page 000");
        page.Header = PageHeader.CreateDefault();

        return project;
    }
}
