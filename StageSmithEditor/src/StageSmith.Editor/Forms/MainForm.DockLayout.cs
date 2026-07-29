using StageSmith.Editor.DockContents;
using WeifenLuo.WinFormsUI.Docking;

namespace StageSmith.Editor;

public partial class MainForm
{
    private const string LayoutFileName = "SSE_Layout.xml";
    private static string LayoutFilePath => Path.Combine(AppContext.BaseDirectory, LayoutFileName);

    // =========================
    // DockLayout 初期化
    // =========================
    private void InitializeDockLayout()
    {
        if (!_stageExplorerContent.IsDisposed)
        {
            _stageExplorerContent.Show(_dockPanel, DockState.DockLeft);
        }

        if (!_bookmarkListContent.IsDisposed &&
            !_stageExplorerContent.IsDisposed &&
            _stageExplorerContent.Pane != null)
        {
            _bookmarkListContent.Show(_stageExplorerContent.Pane, null);
        }

        if (!_metaTilePaletteContent.IsDisposed &&
            !_stageExplorerContent.IsDisposed &&
            _stageExplorerContent.Pane != null)
        {
            _metaTilePaletteContent.Show(_stageExplorerContent.Pane, DockAlignment.Bottom, 0.35);
        }

        if (!_propertyWindowContent.IsDisposed)
        {
            _propertyWindowContent.Show(_dockPanel, DockState.DockRight);
        }

        if (!_mapViewContent.IsDisposed)
        {
            _mapViewContent.Show(_dockPanel, DockState.Document);
            _mapViewContent.UpdateZoomTitle(_mapView.ZoomScale);
        }

        _stageExplorerContent.Activate();
    }

    private void ShowDockContent(DockContent content, DockState dockState)
    {
        if (content.IsDisposed)
        {
            MessageBox.Show(
                $"{content.Text} は破棄されています。アプリを再起動してください。",
                "Window",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning
            );
            return;
        }

        if (content.DockPanel == null)
        {
            content.Show(_dockPanel, dockState);
        }
        else
        {
            content.Show();
            content.Activate();
        }
    }

    private void ShowMapViewContent()
    {
        ShowDockContent(_mapViewContent, DockState.Document);
        _mapViewContent.UpdateZoomTitle(_mapView.ZoomScale);
    }

    private void ResetDockLayout()
    {
        _dockPanel.SuspendLayout(true);

        try
        {
            HideDockContent(_mapViewContent);
            HideDockContent(_stageExplorerContent);
            HideDockContent(_bookmarkListContent);
            HideDockContent(_propertyWindowContent);
            HideDockContent(_metaTilePaletteContent);

            InitializeDockLayout();

            _mapViewContent.UpdateZoomTitle(_mapView.ZoomScale);

            ResetMainWindowBounds();
        }
        finally
        {
            _dockPanel.ResumeLayout(true, true);
        }
    }

    private static void HideDockContent(DockContent content)
    {
        if (content.IsDisposed)
            return;

        if (content.DockPanel != null)
            content.Hide();
    }

    /// <summary>
    /// 起動時専用。保存済みレイアウトがあれば復元し、無ければ／破損していればデフォルト配置にフォールバックする。
    /// Reset Window Layout（既存の InitializeDockLayout 呼び出し）はこの対象外とし、常にデフォルト配置を再構築する。
    /// </summary>
    private void InitializeDockLayoutFromSavedOrDefault()
    {
        if (File.Exists(LayoutFilePath))
        {
            try
            {
                _dockPanel.LoadFromXml(LayoutFilePath, DeserializeDockContent);
                _mapViewContent.UpdateZoomTitle(_mapView.ZoomScale);
                return;
            }
            catch
            {
                // 破損 or 非互換レイアウト → デフォルトへフォールバック
            }
        }

        InitializeDockLayout();
    }

    private IDockContent? DeserializeDockContent(string persistString)
    {
        if (persistString == typeof(MapViewContent).ToString()) return _mapViewContent;
        if (persistString == typeof(StageExplorerContent).ToString()) return _stageExplorerContent;
        if (persistString == typeof(PropertyWindowContent).ToString()) return _propertyWindowContent;
        if (persistString == typeof(MetaTilePaletteContent).ToString()) return _metaTilePaletteContent;
        if (persistString == typeof(BookmarkListContent).ToString()) return _bookmarkListContent;
        if (persistString == typeof(MarkerColorPanelContent).ToString()) return _markerColorPanelContent;
        return null;
    }

    private void SaveDockLayout()
    {
        try
        {
            _dockPanel.SaveAsXml(LayoutFilePath);
        }
        catch
        {
            // 保存失敗は握りつぶす（次回起動時はデフォルトへフォールバックするだけなので致命的ではない）
        }
    }
}
