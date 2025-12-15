using Dalichrome.RandomGenerator.Configs;
using Dalichrome.RandomGenerator.Core;
using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Tilemaps;

#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace Dalichrome.RandomGenerator.UserData
{
    [CreateAssetMenu(menuName = "RandomGenerator/UserData/TileObject")]
    public class TileObject : AbstractUserData
    {
        [Header("Core Fields")]
        [SerializeField, TileTypeCollision] public int id;
        [SerializeField] public TileKind tileKind = TileKind.Normal;
#if ODIN_INSPECTOR
        [HideIf("tileKind", TileKind.Table)]
#endif
        [SerializeField, LayerDisplay] public int layer;

        public override int GetId() => id;

        [Header("Generator UI Fields")]
        [SerializeField] public string tileName;
        [SerializeField] public Sprite menuSprite;
#if ODIN_INSPECTOR
        [HideIf("tileKind", TileKind.Table)]
#endif
        [SerializeField] public Color color;

        [Header("Instance Spawning")]
#if ODIN_INSPECTOR
        [InlineProperty, HideLabel]
        [HideIf("tileKind", TileKind.Table)]
#endif
        [SerializeField] public TileSpawn tileSpawn;

#if ODIN_INSPECTOR
        [ShowIf("tileKind", TileKind.Table)]
#endif
        [Header("Tile Table")]
        [SerializeField] public List<TileTableEntry> table;


        [Header("Metadata Variants")]
#if ODIN_INSPECTOR
        [TableList(AlwaysExpanded = true,
                   DrawScrollView = false,
                   MinScrollViewHeight = 0,
                   ShowIndexLabels = true)]

        [HideIf("tileKind", TileKind.Table)]
#endif
        public bool injectVarianceData = false;

#if ODIN_INSPECTOR
        [HideIf("tileKind", TileKind.Table)]
#endif
        [SerializeField] public List<MetaDataTileSpawn> metaDataSpawnList;

        /// <summary>
        /// Pick the appropriate TileSpawn variant for this tile based on the
        /// already-grouped metadata pairs for this tile (3D + 2D/column).
        /// </summary>
        public TileSpawn GetSpawnForMeta(List<MetaPair> metaPairs)
        {
            if (metaDataSpawnList != null && metaDataSpawnList.Count > 0 && metaPairs != null && metaPairs.Count > 0)
            {
                for (int i = 0; i < metaDataSpawnList.Count; i++)
                {
                    var variant = metaDataSpawnList[i];
                    if (variant != null && variant.Matches(metaPairs))
                        return variant.tileSpawn;
                }
            }

            // Fallback: default spawn
            return tileSpawn;
        }

        public List<int2> GetTableAsInt2()
        {
            if (tileKind != TileKind.Table) return new();

            List<int2> intTable = new();
            foreach (var pair in table)
            {
                intTable.Add(new(pair.tileId,pair.weight));
            }
            return intTable;
        }

        // in TileObject
        internal void PrecompileMetaConditions()
        {
            if (metaDataSpawnList == null)
                return;

            for (int i = 0; i < metaDataSpawnList.Count; i++)
            {
                var rule = metaDataSpawnList[i];
                if (rule?.condition != null)
                    rule.condition.Precompile();
            }
        }
    }
}
