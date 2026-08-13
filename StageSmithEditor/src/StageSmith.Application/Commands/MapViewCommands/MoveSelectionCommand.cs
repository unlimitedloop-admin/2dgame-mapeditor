using StageSmith.Core.Models;

namespace StageSmith.Application.Commands;

/// <summary>
/// 選択範囲を移動するコマンドです。
/// </summary>
public class MoveSelectionCommand : ICommand
{
    // REVIEW: このコマンドは現在どこからも参照されていないが、将来的に選択範囲の移動機能を独立化する可能性が考えられるため、残しておく。

    private readonly TileMap _tileMap;
    private readonly Rectangle _srcRect;
    private readonly Rectangle _dstRect;
    private readonly byte[,] _movedTiles;
    private readonly byte[,] _originalSrcTiles;
    private readonly byte[,] _originalDstTiles;
    private readonly byte _clearTileId;

    public MoveSelectionCommand(TileMap tileMap, Rectangle srcRect, Rectangle dstRect, byte clearTileId = 0)
    {
        _tileMap = tileMap;
        _srcRect = srcRect;
        _dstRect = dstRect;
        _clearTileId = clearTileId;

        // 実行前にスナップショットを取っておく
        _movedTiles = Snapshot(tileMap, srcRect);
        _originalSrcTiles = Snapshot(tileMap, srcRect);
        _originalDstTiles = Snapshot(tileMap, dstRect);
    }

    public void Execute()
    {
        Fill(_tileMap, _srcRect, _clearTileId);
        Paste(_tileMap, _dstRect, _movedTiles);
    }

    public void Undo()
    {
        Paste(_tileMap, _dstRect, _originalDstTiles);
        Paste(_tileMap, _srcRect, _originalSrcTiles);
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
