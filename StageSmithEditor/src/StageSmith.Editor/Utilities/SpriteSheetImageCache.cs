using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using StageSmith.Core.Models;

namespace StageSmith.Editor.Utilities;

/// <summary>
/// 色替えの指定。Key が同じなら同じ色替え結果として扱う（キャッシュのキー）。
/// ゲーム側 SpriteAtlas::ReplacePixelColors と同じく、RGB が From と完全一致したピクセルを To に置き換える。
/// </summary>
public sealed record PaletteRecolor(string Key, IReadOnlyList<(Color From, Color To)> Mappings);

/// <summary>
/// 画像シート（SpriteSheet）の Bitmap をパス単位でキャッシュする。
/// オブジェクトパレットとマップビューで同じインスタンスを共有する。
/// 読み込めなかったパスも記録し、描画のたびにファイルアクセスが走らないようにする。
/// 色替え済みの画像も（パス, 色替えKey）単位でキャッシュする。
/// </summary>
public sealed class SpriteSheetImageCache : IDisposable
{
    private readonly Dictionary<string, Bitmap?> _images = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<(string Path, string Key), Bitmap?> _recolored = [];

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
    /// 色替え済みのシート画像を返す。recolor が null または置き換えが無ければ元画像を返す。
    /// </summary>
    public Bitmap? Get(SpriteSheet? sheet, PaletteRecolor? recolor)
    {
        var source = Get(sheet);
        if (source == null || recolor == null || recolor.Mappings.Count == 0)
            return source;

        var key = (sheet!.ImagePath, recolor.Key);
        if (_recolored.TryGetValue(key, out var cached))
            return cached;

        var recolored = Recolor(source, recolor.Mappings);
        _recolored[key] = recolored;
        return recolored;
    }

    /// <summary>
    /// シートの1コマを dstRect に描画する。画像が無い場合は false を返す（呼び出し側で代替表示する）。
    /// </summary>
    public bool DrawTile(Graphics g, SpriteSheet? sheet, int tileIndex, Rectangle dstRect,
        ImageAttributes? attributes = null, PaletteRecolor? recolor = null)
    {
        var image = Get(sheet, recolor);
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
    /// 指定パスのキャッシュ（色替え済みを含む）を破棄する（画像を差し替えた後の再読込用）。
    /// </summary>
    public void Invalidate(string imagePath)
    {
        if (_images.Remove(imagePath, out var image))
            image?.Dispose();

        foreach (var key in _recolored.Keys.Where(k => string.Equals(k.Path, imagePath, StringComparison.OrdinalIgnoreCase)).ToList())
        {
            _recolored[key]?.Dispose();
            _recolored.Remove(key);
        }
    }

    /// <summary>
    /// 色替え済み画像のキャッシュだけを破棄する（敵定義・パレットを読み直したとき用）。
    /// </summary>
    public void ClearRecolored()
    {
        foreach (var image in _recolored.Values)
            image?.Dispose();

        _recolored.Clear();
    }

    public void Clear()
    {
        ClearRecolored();

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

    /// <summary>
    /// RGB が From と完全一致するピクセルを To に置き換えた複製を作る（アルファはそのまま）。
    /// ゲーム側 SpriteAtlas::ReplacePixelColors と同じ規則（最初に一致した置き換えを使う）。
    /// </summary>
    private static Bitmap Recolor(Bitmap source, IReadOnlyList<(Color From, Color To)> mappings)
    {
        // 描画経由だと合成で色が変わりうるため、ピクセル値をそのまま写す Clone で複製する
        var rect = new Rectangle(0, 0, source.Width, source.Height);
        var result = source.Clone(rect, PixelFormat.Format32bppArgb);

        var data = result.LockBits(rect, ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
        try
        {
            var byteCount = data.Stride * data.Height;
            var pixels = new byte[byteCount];
            Marshal.Copy(data.Scan0, pixels, 0, byteCount);

            for (var y = 0; y < data.Height; y++)
            {
                var row = y * data.Stride;
                for (var x = 0; x < data.Width; x++)
                {
                    // Format32bppArgb のメモリ上の並びは B, G, R, A
                    var i = row + x * 4;
                    var b = pixels[i];
                    var gr = pixels[i + 1];
                    var r = pixels[i + 2];

                    foreach (var (from, to) in mappings)
                    {
                        if (r != from.R || gr != from.G || b != from.B) continue;

                        pixels[i] = to.B;
                        pixels[i + 1] = to.G;
                        pixels[i + 2] = to.R;
                        break;
                    }
                }
            }

            Marshal.Copy(pixels, 0, data.Scan0, byteCount);
        }
        finally
        {
            result.UnlockBits(data);
        }

        return result;
    }

    public void Dispose() => Clear();
}
