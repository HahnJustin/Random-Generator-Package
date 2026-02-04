using Dalichrome.RandomGenerator.UserData;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Dalichrome.RandomGenerator.Core;
using Dalichrome.RandomGenerator.Utils;
using Dalichrome.RandomGenerator.Configs;

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

        [NonSerialized] private bool _compiled = false;
        [NonSerialized] private CompiledMetaCondition compiledCondition;

        public void EnsureCompiled()
        {
            if (_compiled) return;

            compiledCondition = MetaConditionCompiler.Compile(condition);
            _compiled = true;
        }

        public bool Matches(List<MetaPair> metaPairs)
        {
            EnsureCompiled();

            return compiledCondition != null && compiledCondition.Matches(metaPairs);
        }
    }
}