using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;
using Dalichrome.RandomGenerator.Core;

namespace Dalichrome.RandomGenerator.Configs
{
    [Serializable]
    public class RoomConnectionConfig : AbstractGeneratorConfig, IRoomConfig
    {
        public RoomConnectionConfig()
        {
            _description = StringType.Description_Generator_RoomConnection;
        }

        public override GeneratorType Type { get { return GeneratorType.Room_Connection; } }

        [TileDisplay] public int HallwayTile { get { return _hallwayTile; } set { _hallwayTile = value; } }
        [TileDisplay, SerializeField] private int _hallwayTile = (int)TileDefaults.Wall_NA;

        [TileDisplay] public List<int> CarveThroughTiles { get { return _carveThroughTiles; } set { _carveThroughTiles = value; } }
        [TileDisplay, SerializeField] private List<int> _carveThroughTiles = new() { (int)TileDefaults.Wall_Cave, (int)TileDefaults.Wall_NA };

        public bool AdditionalConnections { get { return _additionalConnections; } set { _additionalConnections = value; } }
        [SerializeField] private bool _additionalConnections = false;

        [Condition("AdditionalConnections", true)] public int AdditionalUpperBound { get { return _additionalUpperBound; } set { _additionalUpperBound = value; } }
        [Condition("AdditionalConnections", true), SerializeField] private int _additionalUpperBound = 30;

        [Condition("AdditionalConnections", true)] public int AdditionalLowerBound { get { return _additionalLowerBound; } set { _additionalLowerBound = value; } }
        [Condition("AdditionalConnections", true), SerializeField] private int _additionalLowerBound = 20;

        public OccupanceType Occupance { get { return _occupance; } set { _occupance = value; } }
        [SerializeField] protected OccupanceType _occupance = OccupanceType.Wall_Obj_Not_NA;

        [Condition("Occupance", OccupanceType.Layer_Not_NA)] public LayerType OccupyLayer { get { return _occupyLayer; } set { _occupyLayer = value; } }
        [Condition("Occupance", OccupanceType.Layer_Not_NA), SerializeField] protected LayerType _occupyLayer = LayerType.Wall;

        [TileDisplay, Condition("Occupance", OccupanceType.Contains_A)] public int TileA { get { return _tileA; } set { _tileA = value; } }
        [TileDisplay, Condition("Occupance", OccupanceType.Contains_A), SerializeField] protected int _tileA = (int)TileDefaults.Wall_Cave;

        public bool InvertOccupance { get { return _invertOccupance; } set { _invertOccupance = value; } }
        [SerializeField] protected bool _invertOccupance = false;
    }
}