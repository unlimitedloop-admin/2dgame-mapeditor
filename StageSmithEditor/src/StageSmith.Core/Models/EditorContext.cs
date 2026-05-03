namespace StageSmith.Core.Models;

public class EditorContext
{
    //========================
    // Project
    //========================
    public EditorProject? Project { get; set; }

    //========================
    // 現在位置
    //========================
    public int CurrentStageIndex { get; private set; } = 0;
    public int CurrentPageIndex { get; private set; } = 0;

    //========================
    // 参照（安全取得）
    //========================
    public Stage? CurrentStage =>
        Project?.Stages.ElementAtOrDefault(CurrentStageIndex);

    public Page? CurrentPage =>
        CurrentStage?.Pages.ElementAtOrDefault(CurrentPageIndex);

    public TileMap? CurrentTileMap =>
        CurrentPage?.TileMap;

    //========================
    // 操作
    //========================
    public void SetStage(int index)
    {
        if (Project == null) return;
        if (index < 0 || index >= Project.Stages.Count) return;

        CurrentStageIndex = index;
        CurrentPageIndex = 0; // ページリセット
    }

    public void SetPage(int index)
    {
        var stage = CurrentStage;
        if (stage == null) return;
        if (index < 0 || index >= stage.Pages.Count) return;

        CurrentPageIndex = index;
    }

    //========================
    // ユーティリティ
    //========================
    public bool HasProject => Project != null;
    public bool HasStage => CurrentStage != null;
    public bool HasPage => CurrentPage != null;
}
