using Codice.CM.Common;
using Dalichrome.RandomGenerator.Core;
using Dalichrome.RandomGenerator.Utils;
using System.Reflection;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

public struct FillJob : IJobParallelFor
{
    [ReadOnly] public NativeTileGrid readGrid;
    [ReadOnly] public bool useMetaKey;
    [ReadOnly] public int hotMetaIndex;
    [NativeDisableParallelForRestriction] public NativeTileGrid writeGrid;

    [ReadOnly] public NativeArray<int> fillIds;

    public void Execute(int index)
    {
        int2 pos = new(index % readGrid.width, index / readGrid.width);

        for (int i = 0; i < fillIds.Length; i++)
        {
            if (useMetaKey)
            {
                uint s = (uint)math.hash(new int4(
                    (int)readGrid.seed,
                    pos.x * 73856093,
                    pos.y * 19349663,
                    fillIds[i] * 83492791
                ));
                if (s == 0) s = 1;

                var rng = new Unity.Mathematics.Random(s);
                float roll = rng.NextFloat();

                if (roll > readGrid.GetData(pos, hotMetaIndex)) continue;
            }

            writeGrid.SetTileId(pos.x, pos.y, fillIds[i]);
        }
    }
}
