using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Dalichrome.RandomGenerator.Core;

[BurstCompile]
public struct GuaranteeSpawnJob : IJob
{
    /* inputs */
    [ReadOnly] public NativeArray<int2> candidateTiles;
    [ReadOnly] public NativeTileGrid inputGrid;
    [ReadOnly] public NativeArray<int2> tilePairs;   // (id, weight)
    public uint seed;

    /* outputs */
    [NativeDisableParallelForRestriction] public NativeTileGrid outputGrid;
    public NativeList<int2> outputExcludes;

    public int maxSpawns;
    public int minDistance;
    public bool updateMask;
    public bool useEntranceDistance;

    public void Execute()
    {
        var rng = new Random(seed);

        /* total weight */
        int total = 0;
        for (int i = 0; i < tilePairs.Length; ++i)
            total += tilePairs[i].y;          // y = weight

        int placed = 0;
        for (int i = 0; i < candidateTiles.Length && placed < maxSpawns; ++i)
        {
            int2 pos = candidateTiles[i];

            if ((useEntranceDistance && inputGrid.GetTileValue(pos) > -minDistance) ||
                inputGrid.IsExcluding(pos))
                continue;

            /* weighted pick */
            int roll = rng.NextInt(total);  // 0..total-1
            int accum = 0;
            int tileId = tilePairs[0].x;      // fallback

            for (int j = 0; j < tilePairs.Length; ++j)
            {
                accum += tilePairs[j].y;
                if (roll < accum)
                {
                    tileId = tilePairs[j].x;  // x = id
                    break;
                }
            }

            outputGrid.SetTileId(pos, tileId);
            if (updateMask)
                outputExcludes.AddNoResize(pos);

            ++placed;
        }
    }
}
