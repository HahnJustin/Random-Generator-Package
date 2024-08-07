using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.UIElements;

namespace Dalichrome.RandomGenerator.Configs
{
    [Serializable]
    public class EntranceExitConfig : AbstractGeneratorConfig, IOccupanceConfig, IUniversalMaskConfig
    {
        public EntranceExitConfig()
        {
            _description = StringType.Description_Generator_EntranceExit;
        }

        public override GeneratorType Type { get { return GeneratorType.Entrance_Exit; } }

        public EEPositionType Positioning { get { return _position; } set { _position = value; } }
        [SerializeField] protected EEPositionType _position = EEPositionType.Any_Opposite;

        public EEPlaceableType Placeable { get { return _placeable; } set { _placeable = value; } }
        [SerializeField] protected EEPlaceableType _placeable = EEPlaceableType.One_Side_Wall;

        public TileType SpawnInTile { get { return _spawnInTile; } set { _spawnInTile = value; } }
        [SerializeField] private TileType _spawnInTile = TileType.Wall_Cave;

        [Hidden] public bool ShowUniversalMask { get { return _addEntranceExitToMask || (_addPathToMask && _createPath); } }

        public bool AddEntranceExitToMask { get { return _addEntranceExitToMask; } set { _addEntranceExitToMask = value; } }
        [SerializeField] private bool _addEntranceExitToMask = true;

         public bool CreatePath { get { return _createPath; } set { _createPath = value; } }
        [SerializeField] public bool _createPath = false;

        [Condition("CreatePath", true)]public bool DebugPath { get { return _debugPath; } set { _debugPath = value; } }
        [SerializeField] private bool _debugPath = false;

        [Condition("CreatePath", true)] public bool AddPathToMask { get { return _addPathToMask; } set { _addPathToMask = value; } }
        [SerializeField] private bool _addPathToMask = true;

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