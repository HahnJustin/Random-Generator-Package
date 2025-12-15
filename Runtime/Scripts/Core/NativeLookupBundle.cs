using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
namespace Dalichrome.RandomGenerator
{
    public struct NativeLookupBundle : ILookupBundle, IDisposable
    {
        public int valid;

        [ReadOnly] public NativeParallelHashMap<int, int> tileIdToLayerIndexLookup;
        [ReadOnly] public NativeParallelHashMap<int, int> layerIdToLayerIndexLookup;
        [ReadOnly] public NativeParallelHashMap<int, int> tileIdToTileKindLookup;
        [ReadOnly] public NativeArray<int> layerIndexToDefaultOccupanceLookup;
        [ReadOnly] public NativeParallelHashMap<int, TablePointer> tileIdToTableLookup;
        [ReadOnly] public NativeArray<int2> tileTables;

        public void Dispose()
        {
            if (tileIdToLayerIndexLookup.IsCreated) tileIdToLayerIndexLookup.Dispose();
            if (layerIdToLayerIndexLookup.IsCreated) layerIdToLayerIndexLookup.Dispose();
            if (tileIdToTileKindLookup.IsCreated) tileIdToTileKindLookup.Dispose();
            if (layerIndexToDefaultOccupanceLookup.IsCreated) layerIndexToDefaultOccupanceLookup.Dispose();
            if (tileIdToTableLookup.IsCreated) tileIdToTableLookup.Dispose();
            if (tileTables.IsCreated) tileTables.Dispose();
            valid = 0;
        }
    }
}