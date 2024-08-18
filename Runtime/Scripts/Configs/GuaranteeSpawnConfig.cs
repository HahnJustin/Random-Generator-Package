using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;

namespace Dalichrome.RandomGenerator.Configs
{
    [Serializable]
    public class GuaranteeSpawnConfig : AbstractGeneratorConfig, IRoomConfig
    {
        public GuaranteeSpawnConfig()
        {
            _description = StringType.Description_Generator_RoomFill;
        }

        public override GeneratorType Type { get { return GeneratorType.Guarantee_Spawn; } }

        public int MinimumSpawnDistance { get { return _minimumSpawnDistance; } set { _minimumSpawnDistance = value; } }
        [SerializeField] private int _minimumSpawnDistance = 10;

        public int MinimumAmount { get { return _minimumAmount; } set { _minimumAmount = value; } }
        [SerializeField] private int _minimumAmount = 5;

        public int MaximumAmount { get { return _maximumAmount; } set { _maximumAmount = value; } }
        [SerializeField] private int _maximumAmount = 10;

        public List<TileType> TileTypes { get { return _tileTypes; } set { _tileTypes = value; } }
        [SerializeField] private List<TileType> _tileTypes = new List<TileType>() { TileType.Object_Sack_Grub };

        public OccupanceType Occupance { get { return _occupance; } set { _occupance = value; } }
        [SerializeField] protected OccupanceType _occupance = OccupanceType.Wall_Obj_Not_NA;

        [Condition("Occupance", OccupanceType.Layer_Not_NA)] public LayerType OccupyLayer { get { return _occupyLayer; } set { _occupyLayer = value; } }
        [SerializeField] protected LayerType _occupyLayer = LayerType.Wall;

        [Condition("Occupance", OccupanceType.Contains_A)] public TileType TileA { get { return _tileA; } set { _tileA = value; } }
        [SerializeField] protected TileType _tileA = TileType.Wall_Cave;

        public bool InvertOccupance { get { return _invertOccupance; } set { _invertOccupance = value; } }
        [SerializeField] protected bool _invertOccupance = false;
    }
}