using Dalichrome.RandomGenerator.Core;
using Dalichrome.RandomGenerator.Utils;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Collections;

public struct FillJob : IJobParallelFor
{
    [ReadOnly] public NativeTileGrid readGrid;
    [NativeDisableParallelForRestriction] public NativeTileGrid writeGrid;

    [ReadOnly] public NativeArray<int> fillIds;

    public void Execute(int index)
    {
        int2 pos = new(index % readGrid.width, index / readGrid.width);

        for (int i = 0; i < fillIds.Length; i++)
        {
            writeGrid.SetTileId(pos, fillIds[i]);
        }
    }
}
