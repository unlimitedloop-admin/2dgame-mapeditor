using System.Drawing;

namespace StageSmith.Editor.Utilities;

/// <summary>
/// カスタムカーソルを生成するファクトリクラス。
/// </summary>
public static class CursorFactory
{
    /// <summary>
    /// PNG ファイルからホットスポット付きのカーソルを生成する。
    /// </summary>
    /// <param name="pngPath">PNG ファイルのパス。</param>
    /// <param name="hotspotX">ホットスポットの X 座標。</param>
    /// <param name="hotspotY">ホットスポットの Y 座標。</param>
    /// <returns>生成された <see cref="Cursor"/>。</returns>
    public static Cursor FromPng(string pngPath, int hotspotX = 0, int hotspotY = 0)
    {
        using var bitmap = new Bitmap(pngPath);
        IntPtr hIcon = bitmap.GetHicon();

        var iconInfo = new NativeMethods.IconInfo();
        NativeMethods.GetIconInfo(hIcon, ref iconInfo);
        iconInfo.xHotspot = hotspotX;
        iconInfo.yHotspot = hotspotY;
        iconInfo.fIcon = false; // カーソルとして使用

        IntPtr hCursor = NativeMethods.CreateIconIndirect(ref iconInfo);

        NativeMethods.DeleteObject(iconInfo.hbmMask);
        NativeMethods.DeleteObject(iconInfo.hbmColor);
        NativeMethods.DestroyIcon(hIcon);

        return new Cursor(hCursor);
    }
}
