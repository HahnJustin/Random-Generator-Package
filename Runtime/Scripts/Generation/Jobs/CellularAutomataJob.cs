using Dalichrome.RandomGenerator.Core;
using Dalichrome.RandomGenerator.Utils;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Collections;

public struct CellularAutomataJob : IJobParallelFor
{
    [ReadOnly] public NativeTileGrid readGrid;
    [NativeDisableParallelForRestriction] public NativeTileGrid writeGrid;

    public OccupanceData occupance;

    public int liveNeighborsRequired;
    public float placeProbability;
    public int fillId;
    public int emptyId;
    public uint baseSeed;
    public uint repetition;

    public void Execute(int index)
    {
        if (repetition >= 1) occupance.SetInvert(false);

        int2 pos = new(index % readGrid.width, index / readGrid.width);
        var rng = new Random(baseSeed + (uint)index + repetition);

        int occupied = occupance.IsOccupied(pos, readGrid);
        int neighbors = CountLiveNeighbors(pos);

        bool shouldLive = (occupied + neighbors) >= liveNeighborsRequired;
        bool place = placeProbability >= 1f || rng.NextFloat() < placeProbability;

        writeGrid.SetTileId(pos.x, pos.y, shouldLive && place ? fillId : emptyId);
    }

    private int CountLiveNeighbors(int2 pos)
    {
        int count = 0;
        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0) continue;

                int2 neighbor = pos + new int2(dx, dy);
                count += occupance.IsOccupied(neighbor, readGrid);
            }
        }
        return count;
    }
}
