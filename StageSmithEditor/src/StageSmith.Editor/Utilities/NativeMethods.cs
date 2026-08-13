using System.Runtime.InteropServices;

namespace StageSmith.Editor.Utilities;

/// <summary>
/// Win32 API のネイティブメソッドをまとめたクラス。
/// </summary>
internal static class NativeMethods
{
    [StructLayout(LayoutKind.Sequential)]
    public struct IconInfo
    {
        public bool fIcon;
        public int xHotspot;
        public int yHotspot;
        public IntPtr hbmMask;
        public IntPtr hbmColor;
    }

    [DllImport("user32.dll")]
    public static extern bool GetIconInfo(IntPtr hIcon, ref IconInfo pIconInfo);

    [DllImport("user32.dll")]
    public static extern IntPtr CreateIconIndirect(ref IconInfo icon);

    [DllImport("user32.dll")]
    public static extern bool DestroyIcon(IntPtr hIcon);

    [DllImport("gdi32.dll")]
    public static extern bool DeleteObject(IntPtr hObject);
}
