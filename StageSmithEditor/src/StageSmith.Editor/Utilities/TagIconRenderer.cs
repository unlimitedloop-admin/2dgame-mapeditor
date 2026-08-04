namespace StageSmith.Editor.Utilities;

/// <summary>
/// タグアイコンの描画・読み込みを統一する。
/// タイル系描画（NearestNeighbor＝ドット絵の再現性重視）とは目的が違うため、
/// タグアイコンは通常の画像として滑らかに縮小表示する（HighQualityBicubic）。
/// </summary>
public static class TagIconRenderer
{
    public static void Draw(Graphics g, Image icon, Rectangle destRect)
    {
        g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
        g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
        g.DrawImage(icon, destRect);
    }

    /// <summary>アイコン画像を安全に読み込む。存在しない/壊れている場合はnull。</summary>
    public static Image? SafeLoad(string? fullPath)
    {
        if (string.IsNullOrWhiteSpace(fullPath) || !File.Exists(fullPath))
            return null;

        try { return Image.FromFile(fullPath); }
        catch { return null; }
    }
}
