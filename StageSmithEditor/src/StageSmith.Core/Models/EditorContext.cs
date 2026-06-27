namespace StageSmith.Core.Models;

public enum EditorBrushKind
{
    Tile,
    MetaTile
}

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
    public Stage? CurrentStage => Project?.Stages.ElementAtOrDefault(CurrentStageIndex);

    public Page? CurrentPage => CurrentStage?.Pages.ElementAtOrDefault(CurrentPageIndex);

    public TileMap? CurrentTileMap => CurrentPage?.TileMap;

    //========================
    // 現在の配置ブラシ
    //========================
    public EditorBrushKind CurrentBrushKind { get; private set; } = EditorBrushKind.Tile;

    public int SelectedTileId { get; private set; } = -1;

    public MetaTile? SelectedMetaTile { get; private set; }

    public event Action? BrushChanged;

    private void NotifyBrushChanged()
    {
        BrushChanged?.Invoke();
    }

    public void SetSelectedTile(int tileId)
    {
        SelectedTileId = tileId;
        SelectedMetaTile = null;
        CurrentBrushKind = EditorBrushKind.Tile;

        NotifyBrushChanged();
    }

    public void SetSelectedMetaTile(MetaTile? metaTile)
    {
        SelectedMetaTile = metaTile;
        CurrentBrushKind = metaTile == null
            ? EditorBrushKind.Tile
            : EditorBrushKind.MetaTile;

        NotifyBrushChanged();
    }

    //========================
    // 操作
    //========================
    public event Action? ContextChanged;

    private void NotifyChanged()
    {
        ContextChanged?.Invoke();
    }

    public void SetStage(int index)
    {
        if (Project == null) return;
        if (index < 0 || index >= Project.Stages.Count) return;

        CurrentStageIndex = index;
        CurrentPageIndex = 0; // ページリセット

        NotifyChanged();
    }

    public void SetPage(int index)
    {
        var stage = CurrentStage;
        if (stage == null) return;
        if (index < 0 || index >= stage.Pages.Count) return;

        CurrentPageIndex = index;

        NotifyChanged();
    }

    public void MovePrevPage()
    {
        if (CurrentPageIndex > 0)
            SetPage(CurrentPageIndex - 1);
    }

    public void MoveNextPage()
    {
        var stage = CurrentStage;
        if (stage == null) return;
        if (CurrentPageIndex < stage.Pages.Count - 1)
            SetPage(CurrentPageIndex + 1);
    }

    public void MoveFirstPage() => SetPage(0);

    public void MoveLastPage()
    {
        var stage = CurrentStage;
        if (stage == null) return;
        SetPage(stage.Pages.Count - 1);
    }

    public int PageCount => CurrentStage?.Pages.Count ?? 0;

    //========================
    // ユーティリティ
    //========================
    public bool HasProject => Project != null;
    public bool HasStage => CurrentStage != null;
    public bool HasPage => CurrentPage != null;

}
