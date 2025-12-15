using Dalichrome.RandomGenerator.UserData;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Dalichrome.RandomGenerator.Core;
using Dalichrome.RandomGenerator.Configs;
using Unity.Mathematics;




#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace Dalichrome.RandomGenerator.UserData
{
    [Serializable]
    public class TileTableEntry
    {
#if ODIN_INSPECTOR
        // First column: condition, no header text
        [InlineProperty, HideLabel]
        [TableColumnWidth(380, Resizable = true)]
#endif
        [TileDisplay] public int tileId;

#if ODIN_INSPECTOR
        // Second column: tile spawn, no header text
        [InlineProperty]
        [TableColumnWidth(220, Resizable = true)]
#endif
        public int weight;

        public int2 Int2 => new int2(tileId, weight);
    }
}