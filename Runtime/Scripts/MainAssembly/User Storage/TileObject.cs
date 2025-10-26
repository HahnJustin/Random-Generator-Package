
using Dalichrome.RandomGenerator.Configs;
using Dalichrome.RandomGenerator.Core;
using System;
using System.Collections.Generic;
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
        [SerializeField,TileTypeCollision] public int id;
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
    }

    public enum TileSpawnType
    {
        Sprite,
        TileBase,
        GameObject
    }

    [Serializable]
    public class TileSpawn
    {
        [SerializeField] public TileSpawnType spawnType;

        [SerializeField, Condition(nameof(spawnType), TileSpawnType.Sprite)]
        public Sprite sprite;

        [SerializeField, Condition(nameof(spawnType), TileSpawnType.TileBase)]
        public TileBase tileBase;

        [SerializeField, Condition(nameof(spawnType), TileSpawnType.GameObject)]
        public GameObject gameObject;
    }

    [Serializable]
    public class MetaDataTileSpawn
    {
#if ODIN_INSPECTOR
        [InlineProperty, HideLabel]
#endif
        [SerializeField] public SerialPair<string,string> metaDataToMatch;
#if ODIN_INSPECTOR
        [InlineProperty, HideLabel]
#endif
        [SerializeField] public TileSpawn tileSpawn;
    }
}
