using StageSmith.Core.Models;

namespace StageSmith.Editor.Utilities;

/// <summary>
/// 画像シート（SpriteSheet）の Bitmap をパス単位でキャッシュする。
/// オブジェクトパレットとマップビューで同じインスタンスを共有する。
/// 読み込めなかったパスも記録し、描画のたびにファイルアクセスが走らないようにする。
/// </summary>
public sealed class SpriteSheetImageCache : IDisposable
{
    private readonly Dictionary<string, Bitmap?> _images = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// シート画像を返す。ファイルが無い・読めない場合は null。
    /// </summary>
    public Bitmap? Get(SpriteSheet? sheet)
    {
        if (sheet == null || string.IsNullOrWhiteSpace(sheet.ImagePath))
            return null;

        if (_images.TryGetValue(sheet.ImagePath, out var cached))
            return cached;

        var loaded = Load(sheet.ImagePath);
        _images[sheet.ImagePath] = loaded;
        return loaded;
    }

    /// <summary>
    /// シートの1コマを dstRect に描画する。画像が無い場合は false を返す（呼び出し側で代替表示する）。
    /// </summary>
    public bool DrawTile(Graphics g, SpriteSheet? sheet, int tileIndex, Rectangle dstRect,
        System.Drawing.Imaging.ImageAttributes? attributes = null)
    {
        var image = Get(sheet);
        if (image == null || sheet == null || !sheet.IsValidTileIndex(tileIndex))
            return false;

        var (sx, sy, sw, sh) = sheet.GetTileRect(tileIndex);

        // 定義上のコマが画像からはみ出している場合は描かない（GDI+が例外を投げるため）
        if (sx + sw > image.Width || sy + sh > image.Height)
            return false;

        g.DrawImage(image, dstRect, sx, sy, sw, sh, GraphicsUnit.Pixel, attributes);
        return true;
    }

    /// <summary>
    /// 指定パスのキャッシュを破棄する（画像を差し替えた後の再読込用）。
    /// </summary>
    public void Invalidate(string imagePath)
    {
        if (_images.Remove(imagePath, out var image))
            image?.Dispose();
    }

    public void Clear()
    {
        foreach (var image in _images.Values)
            image?.Dispose();

        _images.Clear();
    }

    private static Bitmap? Load(string path)
    {
        if (!File.Exists(path))
            return null;

        try
        {
            // ファイルをロックし続けないよう、読み込んだ画像を複製して保持する
            using var loaded = new Bitmap(path);
            return new Bitmap(loaded);
        }
        catch (Exception ex) when (ex is ArgumentException or IOException or OutOfMemoryException)
        {
            return null;
        }
    }

    public void Dispose() => Clear();
}
