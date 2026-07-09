namespace StageSmith.Editor.Utilities;

/// <summary>
/// タイルセット画像(Bitmap)を安全に保持するためのヘルパー。
/// 渡された画像を内部で複製して所有することで、呼び出し元が
/// オリジナルの Bitmap を Dispose しても、このホルダーが持つ画像には影響しない。
/// 複製に失敗した場合や、保持中の画像が何らかの理由で使用不能になった場合は
/// Current が null を返す。
/// </summary>
public sealed class SafeTilesetHolder : IDisposable
{
    private Bitmap? _tileset;
    private bool _ownsTileset;

    /// <summary>
    /// 現在保持している、描画に使用可能なタイルセット。
    /// 未設定または破棄済み等で使用不能な場合は null。
    /// </summary>
    public Bitmap? Current => GetUsable();

    /// <summary>
    /// 新しいタイルセットをセットする（内部で複製して保持する）。
    /// null を渡すと保持中の画像を破棄してクリアする。
    /// </summary>
    public void Replace(Bitmap? source)
    {
        DisposeOwned();

        if (source == null)
            return;

        try
        {
            _tileset = new Bitmap(source);
            _ownsTileset = true;
        }
        catch (ArgumentException)
        {
            _tileset = null;
            _ownsTileset = false;
        }
        catch (ObjectDisposedException)
        {
            _tileset = null;
            _ownsTileset = false;
        }
    }

    private Bitmap? GetUsable()
    {
        var tileset = _tileset;

        if (tileset == null)
            return null;

        return IsUsable(tileset) ? tileset : null;
    }

    private static bool IsUsable(Bitmap bitmap)
    {
        try
        {
            _ = bitmap.Width;
            _ = bitmap.Height;
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
        catch (ObjectDisposedException)
        {
            return false;
        }
    }

    private void DisposeOwned()
    {
        if (_ownsTileset)
        {
            _tileset?.Dispose();
        }

        _tileset = null;
        _ownsTileset = false;
    }

    public void Dispose()
    {
        DisposeOwned();
    }
}
