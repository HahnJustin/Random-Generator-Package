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
        [SerializeField, LayerDisplay] public int layer;
        [SerializeField] public TileKind tileKind = TileKind.Normal;

        public override int GetId() => id;

        [Header("Generator UI Fields")]
        [SerializeField] public string tileName;
        [SerializeField] public Sprite menuSprite;
        [SerializeField] public Color color;

        [Header("Instance Spawning")]
#if ODIN_INSPECTOR
        [InlineProperty, HideLabel]
#endif
        [SerializeField] public TileSpawn tileSpawn;

        [Header("Metadata Variants")]
#if ODIN_INSPECTOR
        [TableList(AlwaysExpanded = true,
                   DrawScrollView = false,
                   MinScrollViewHeight = 0,
                   ShowIndexLabels = true)]
#endif
        [SerializeField] public List<MetaDataTileSpawn> metaDataSpawnList;

        /// <summary>
        /// Pick the appropriate TileSpawn variant for this tile based on the
        /// already-grouped metadata pairs for this tile (3D + 2D/column).
        /// </summary>
        public TileSpawn GetSpawnForMeta(List<MetaPair> metaPairs)
        {
            if (metaPairs == null || metaPairs.Count == 0 ||
                metaDataSpawnList == null || metaDataSpawnList.Count == 0)
            {
                return tileSpawn;
            }

            // Build a logical metadata context: field -> value
            var context = new Dictionary<string, int>(metaPairs.Count);
            for (int i = 0; i < metaPairs.Count; i++)
            {
                var p = metaPairs[i];
                context[p.field] = p.value;
            }

            // First matching variant wins
            for (int i = 0; i < metaDataSpawnList.Count; i++)
            {
                var variant = metaDataSpawnList[i];
                if (variant == null)
                    continue;

                if (variant.Matches(context))
                    return variant.tileSpawn;
            }

            return tileSpawn;
        }
    }
}
