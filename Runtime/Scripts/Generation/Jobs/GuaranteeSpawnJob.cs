using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Dalichrome.RandomGenerator.Core;

[BurstCompile]
public struct GuaranteeSpawnJob : IJob
{
    /* „Ÿ„Ÿ„Ÿ„Ÿ„Ÿ inputs „Ÿ„Ÿ„Ÿ„Ÿ„Ÿ */
    [ReadOnly] public NativeArray<int2> candidateTiles;
    [ReadOnly] public TileGridData inputGrid;
    [ReadOnly] public NativeArray<int> tileTypes;
    public uint seed;          // unique per job

    /* „Ÿ„Ÿ„Ÿ„Ÿ„Ÿ outputs „Ÿ„Ÿ„Ÿ„Ÿ„Ÿ */
    [NativeDisableParallelForRestriction] public TileGridData outputGrid;
    public NativeList<int2> outputExcludes;

    public int maxSpawns;
    public int minDistance;
    public bool updateMask;
    public bool useEntranceDistance;

    public void Execute()
    {
        var rng = new Random(seed);        // deterministic per job run
        int placed = 0;

        for (int i = 0; i < candidateTiles.Length && placed < maxSpawns; ++i)
        {
            int2 pos = candidateTiles[i];
            Tile tile = inputGrid.GetTile(pos);

            if ((useEntranceDistance && tile.Value > -minDistance) ||
                inputGrid.IsExcluding(pos))
                continue;

            /* pick a new random ID for this spawn */
            int tileId = tileTypes[rng.NextInt(tileTypes.Length)];

            outputGrid.SetTileId(pos, tileId);
            if (updateMask)
                outputExcludes.AddNoResize(pos);

            ++placed;
        }
    }
}