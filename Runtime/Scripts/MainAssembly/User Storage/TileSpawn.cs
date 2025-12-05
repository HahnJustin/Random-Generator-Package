using Dalichrome.RandomGenerator.Configs;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Dalichrome.RandomGenerator.UserData
{
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
}