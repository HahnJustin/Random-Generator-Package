using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;
using Dalichrome.RandomGenerator.Core;

namespace Dalichrome.RandomGenerator.Configs
{
    [Serializable]
    public class GuaranteeSpawnConfig : AbstractGeneratorConfig, IRoomConfig, IUniversalMaskConfig
    {
        public GuaranteeSpawnConfig()
        {
            _description = StringType.Description_Generator_GuaranteedSpawn;
        }

        public override GeneratorType Type { get { return GeneratorType.Guarantee_Spawn; } }

        [Hidden] public bool ShowUniversalMask { get { return _addSpawnsToMask; } }

        public bool UseEntranceDistance { get { return _useEntranceDistance; } set { _useEntranceDistance = value; } }
        [SerializeField] private bool _useEntranceDistance = false;

        [Condition("UseEntranceDistance", true)] public int MinimumDistanceFromEntrance { get { return _minimumSpawnDistance; } set { _minimumSpawnDistance = value; } }
        [Condition("UseEntranceDistance", true), SerializeField] private int _minimumSpawnDistance = 10;

        public int MinimumAmount { get { return _minimumAmount; } set { _minimumAmount = value; } }
        [SerializeField] private int _minimumAmount = 5;

        public int MaximumAmount { get { return _maximumAmount; } set { _maximumAmount = value; } }
        [SerializeField] private int _maximumAmount = 10;

        [TilePairDisplay(showValue:false)] public List<SerialPair<int,int>> TileWeights { get { return _tileWeights; } set { _tileWeights = value; } }
        [TilePairDisplay(showValue: false), SerializeField] private List<SerialPair<int, int>> _tileWeights = new () { new((int)TileDefaults.Object_Sack_Grub, 1) };

        public bool AddSpawnsToMask { get { return _addSpawnsToMask; } set { _addSpawnsToMask = value; } }
        [SerializeField] private bool _addSpawnsToMask = true;

        public OccupanceType Occupance { get { return _occupance; } set { _occupance = value; } }
        [SerializeField] protected OccupanceType _occupance = OccupanceType.Wall_Obj_Not_NA;

        [Condition("Occupance", OccupanceType.Layer_Not_NA)] public LayerType OccupyLayer { get { return _occupyLayer; } set { _occupyLayer = value; } }
        [Condition("Occupance", OccupanceType.Layer_Not_NA), SerializeField] protected LayerType _occupyLayer = LayerType.Wall;

        [TileDisplay, Condition("Occupance", OccupanceType.Contains_A)] public int TileA { get { return _tileA; } set { _tileA = value; } }
        [TileDisplay, Condition("Occupance", OccupanceType.Contains_A), SerializeField] protected int _tileA = (int)TileDefaults.Wall_Cave;

        public bool InvertOccupance { get { return _invertOccupance; } set { _invertOccupance = value; } }
        [SerializeField] protected bool _invertOccupance = false;

        public bool ConfigureFillTile => false;

        public int FillTile { get { return 0; } set { } }
    }
}