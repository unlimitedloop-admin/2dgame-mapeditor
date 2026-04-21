using StageSmith.Core.Models;

namespace StageSmith.Application.Commands;

public class MoveSelectionCommand : ICommand
{
    private readonly TileMap _tileMap;
    private readonly Rectangle _srcRect;
    private readonly Rectangle _dstRect;
    private readonly byte[,] _movedTiles;
    private readonly byte[,] _originalSrcTiles;
    private readonly byte[,] _originalDstTiles;

    public MoveSelectionCommand(TileMap tileMap, Rectangle srcRect, Rectangle dstRect)
    {
        _tileMap = tileMap;
        _srcRect = srcRect;
        _dstRect = dstRect;

        // 実行前にスナップショットを取っておく
        _movedTiles = Snapshot(tileMap, srcRect);
        _originalSrcTiles = Snapshot(tileMap, srcRect);
        _originalDstTiles = Snapshot(tileMap, dstRect);
    }

    public void Execute()
    {
        Fill(_tileMap, _srcRect, 0);               // 元位置を0に
        Paste(_tileMap, _dstRect, _movedTiles);    // 新位置に書き込む
    }

    public void Undo()
    {
        Paste(_tileMap, _dstRect, _originalDstTiles); // 新位置を元に戻す
        Paste(_tileMap, _srcRect, _originalSrcTiles); // 元位置を復元
    }

    private static byte[,] Snapshot(TileMap map, Rectangle rect)
    {
        var tiles = new byte[rect.Width, rect.Height];
        for (var y = 0; y < rect.Height; y++)
            for (var x = 0; x < rect.Width; x++)
                tiles[x, y] = map.GetTile(rect.X + x, rect.Y + y);
        return tiles;
    }

    private static void Fill(TileMap map, Rectangle rect, byte value)
    {
        for (var y = 0; y < rect.Height; y++)
            for (var x = 0; x < rect.Width; x++)
                map.SetTile(rect.X + x, rect.Y + y, value);
    }

    private static void Paste(TileMap map, Rectangle rect, byte[,] tiles)
    {
        for (var y = 0; y < rect.Height; y++)
            for (var x = 0; x < rect.Width; x++)
                map.SetTile(rect.X + x, rect.Y + y, tiles[x, y]);
    }
}
