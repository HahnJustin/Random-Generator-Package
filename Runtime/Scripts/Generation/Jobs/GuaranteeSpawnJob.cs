using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using Dalichrome.RandomGenerator.Core;
using Dalichrome.RandomGenerator.Generators;
using System.Threading;

[BurstCompile]
public unsafe struct GuaranteeSpawnJob : IJobParallelFor
{
    [ReadOnly] public NativeArray<int2> candidateTiles;
    [ReadOnly] public TileGridData inputGrid;
    [NativeDisableParallelForRestriction] public TileGridData outputGrid;

    [ReadOnly] public NativeArray<TileType> spawnTypes;
    [NativeDisableParallelForRestriction] public NativeReference<int> spawnCounter;

    public NativeList<int2>.ParallelWriter outputExcludes;

    public int maxSpawns;
    public int minDistance;
    public bool updateMask;
    public bool useEntranceDistance;
    public uint seed;

    public void Execute(int index)
    {
        // Early out if we already hit the max
        if (spawnCounter.Value >= maxSpawns) return;

        int2 pos = candidateTiles[index];
        Tile tile = inputGrid.GetTile(pos);

        // Check distance and exclusion rules
        if ((useEntranceDistance && tile.Value > -minDistance) || inputGrid.IsExcluding(pos))
            return;

        // Choose a spawn type randomly
        uint perTileSeed = seed + (uint)(index * 73856093);
        var rng = new Random(perTileSeed);
        TileType chosenType = spawnTypes[rng.NextInt(spawnTypes.Length)];

        // Get unsafe pointer to NativeReference value and use atomic increment
        ref int counter = ref UnsafeUtility.AsRef<int>(
            NativeReferenceUnsafeUtility.GetUnsafePtr(spawnCounter)
        );

        int result = Interlocked.Increment(ref counter);
        if (result <= maxSpawns)
        {
            outputGrid.SetTileId(pos, (int)chosenType);
            if (updateMask)
                outputExcludes.AddNoResize(pos);
        }
    }
}
