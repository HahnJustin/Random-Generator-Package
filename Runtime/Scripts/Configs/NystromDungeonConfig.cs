using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;

namespace Dalichrome.RandomGenerator.Configs
{
    [Serializable]
    public class NystromDungeonConfig : AbstractGeneratorConfig, IRoomConfig, IMazeConfig
    {
        public NystromDungeonConfig()
        {
            _description = StringType.Description_Generator_Labyrinth;
        }

        public override GeneratorType Type { get { return GeneratorType.Nystrom_Dungeon; } }

        public float DungeonInRoomOdds { get { return _odds; } set { _odds = value; } }
        [SerializeField] private float _odds = 1f;

        public int RoomPlaceIterations { get { return _iterations; } set { _iterations = value; } }
        [SerializeField] private int _iterations = 100;

        public int RoomMinSize { get { return _roomMin; } set { _roomMin = value; } }
        [SerializeField] private int _roomMin = 3;

        public int RoomMaxSize { get { return _roomMax; } set { _roomMax = value; } }
        [SerializeField] private int _roomMax = 12;

        public TileType FloorTile { get { return _floorTile; } set { _floorTile = value; } }
        [SerializeField] private TileType _floorTile = TileType.Ground_Cobble;

        public TileType WallTile { get { return _wallTile; } set { _wallTile = value; } }
        [SerializeField] private TileType _wallTile = TileType.Wall_Cobble;

        public TileType HallwayTile { get { return _hallwayTile; } set { _hallwayTile = value; } }
        [SerializeField] private TileType _hallwayTile = TileType.Wall_Object_NA;

        public TileType DoorTile { get { return _doorTile; } set { _doorTile = value; } }
        [SerializeField] private TileType _doorTile = TileType.Object_Door;

        public float ExtraDoorOdds { get { return _extraDoorOdds; } set { _extraDoorOdds = value; } }
        [SerializeField] private float _extraDoorOdds = 0.02f;

        public bool PruneDeadends { get { return _pruneDeadends; } set { _pruneDeadends = value; } }
        [SerializeField] private bool _pruneDeadends = true;

        public OccupanceType Occupance { get { return _occupance; } set { _occupance = value; } }
        [SerializeField] protected OccupanceType _occupance = OccupanceType.Wall_Obj_Not_NA;

        [Condition("Occupance", OccupanceType.Layer_Not_NA)] public LayerType OccupyLayer { get { return _occupyLayer; } set { _occupyLayer = value; } }
        [SerializeField, ShowIf("Occupance", OccupanceType.Layer_Not_NA)] protected LayerType _occupyLayer = LayerType.Wall;

        [Condition("Occupance", OccupanceType.Contains_A)] public TileType TileA { get { return _tileA; } set { _tileA = value; } }
        [SerializeField, ShowIf("Occupance", OccupanceType.Contains_A)] protected TileType _tileA = TileType.Wall_Cave;

        public bool InvertOccupance { get { return _invertOccupance; } set { _invertOccupance = value; } }
        [SerializeField] protected bool _invertOccupance = true;
    }
}