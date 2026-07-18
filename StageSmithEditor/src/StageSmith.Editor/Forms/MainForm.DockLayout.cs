using WeifenLuo.WinFormsUI.Docking;

namespace StageSmith.Editor;

public partial class MainForm
{
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
}
