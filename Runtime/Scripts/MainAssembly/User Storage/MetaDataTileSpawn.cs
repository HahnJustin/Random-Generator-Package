using Dalichrome.RandomGenerator.UserData;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace Dalichrome.RandomGenerator.UserData
{
    [Serializable]
    public class MetaDataTileSpawn
    {
#if ODIN_INSPECTOR
        // First column: condition, no header text
        [InlineProperty, HideLabel]
        [TableColumnWidth(380, Resizable = true)]
#endif
        public MetaCondition condition;

#if ODIN_INSPECTOR
        // Second column: tile spawn, no header text
        [InlineProperty, HideLabel]
        [TableColumnWidth(220, Resizable = true)]
#endif
        public TileSpawn tileSpawn;

        public bool Matches(IReadOnlyDictionary<string, int> context)
        {
            return condition != null && condition.Matches(context);
        }
    }
}