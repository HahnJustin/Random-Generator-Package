using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

namespace Dalichrome.RandomGenerator
{
    public struct SerialLookupBundle : ILookupBundle
    {
        public int valid;

        [ReadOnly] public IReadOnlyDictionary<int, int> tileIdToLayerIndexLookup;
        [ReadOnly] public IReadOnlyDictionary<int, int> layerIdToLayerIndexLookup;
        [ReadOnly] public IReadOnlyDictionary<int, int> tileIdToTileKindLookup;
        [ReadOnly] public IReadOnlyList<int> layerIndexToDefaultOccupanceLookup;
    }
}
