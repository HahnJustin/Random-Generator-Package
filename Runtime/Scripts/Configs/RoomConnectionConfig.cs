using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;

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

        public TileType HallwayTile { get { return _hallwayTile; } set { _hallwayTile = value; } }
        [SerializeField] private TileType _hallwayTile = TileType.Wall_NA;

        public List<TileType> CarveThroughTiles { get { return _carveThroughTiles; } set { _carveThroughTiles = value; } }
        [SerializeField] private List<TileType> _carveThroughTiles = new() { TileType.Wall_Cave, TileType.Wall_NA };

        public bool AdditionalConnections { get { return _additionalConnections; } set { _additionalConnections = value; } }
        [SerializeField] private bool _additionalConnections = false;

        [Condition("AdditionalConnections", true)] public int AdditionalUpperBound { get { return _additionalUpperBound; } set { _additionalUpperBound = value; } }
        [SerializeField] private int _additionalUpperBound = 30;

        [Condition("AdditionalConnections", true)] public int AdditionalLowerBound { get { return _additionalLowerBound; } set { _additionalLowerBound = value; } }
        [SerializeField] private int _additionalLowerBound = 20;

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