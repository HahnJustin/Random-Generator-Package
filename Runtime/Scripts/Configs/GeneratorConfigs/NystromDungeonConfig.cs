using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;
using Dalichrome.RandomGenerator.Core;

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

        [TileDisplay] public int FloorTile { get { return _floorTile; } set { _floorTile = value; } }
        [TileDisplay, SerializeField] private int _floorTile = (int)TileType.Ground_Cobble;

        [TileDisplay] public int WallTile { get { return _wallTile; } set { _wallTile = value; } }
        [TileDisplay, SerializeField] private int _wallTile = (int)TileType.Wall_Cobble;

        [TileDisplay] public int HallwayTile { get { return _hallwayTile; } set { _hallwayTile = value; } }
        [TileDisplay, SerializeField] private int _hallwayTile = (int)TileType.Wall_Object_NA;

        [TileDisplay] public int DoorTile { get { return _doorTile; } set { _doorTile = value; } }
        [TileDisplay, SerializeField] private int _doorTile = (int)TileType.Object_Door;

        public float ExtraDoorOdds { get { return _extraDoorOdds; } set { _extraDoorOdds = value; } }
        [SerializeField] private float _extraDoorOdds = 0.02f;

        public bool PruneDeadends { get { return _pruneDeadends; } set { _pruneDeadends = value; } }
        [SerializeField] private bool _pruneDeadends = true;

        public OccupanceType Occupance { get { return _occupance; } set { _occupance = value; } }
        [SerializeField] protected OccupanceType _occupance = OccupanceType.Wall_Obj_Not_NA;

        [Condition("Occupance", OccupanceType.Layer_Not_NA)] public LayerType OccupyLayer { get { return _occupyLayer; } set { _occupyLayer = value; } }
        [Condition("Occupance", OccupanceType.Layer_Not_NA), SerializeField] protected LayerType _occupyLayer = LayerType.Wall;

        [TileDisplay, Condition("Occupance", OccupanceType.Contains_A)] public int TileA { get { return _tileA; } set { _tileA = value; } }
        [TileDisplay, Condition("Occupance", OccupanceType.Contains_A), SerializeField] protected int _tileA = (int)TileType.Wall_Cave;

        public bool InvertOccupance { get { return _invertOccupance; } set { _invertOccupance = value; } }
        [SerializeField] protected bool _invertOccupance = true;
    }
}