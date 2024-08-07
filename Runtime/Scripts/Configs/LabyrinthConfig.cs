using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;

namespace Dalichrome.RandomGenerator.Configs
{
    [Serializable]
    public class LabyrinthConfig : AbstractGeneratorConfig, IRoomConfig, IMazeConfig
    {
        public LabyrinthConfig()
        {
            _description = StringType.Description_Generator_Labyrinth;
        }

        public override GeneratorType Type { get { return GeneratorType.Labyrinth; } }

        public TileType WallTile { get { return _wallTile; } set { _wallTile = value; } }
        [SerializeField] private TileType _wallTile = TileType.Wall_Cave;

        public TileType HallwayTile { get { return _hallwayTile; } set { _hallwayTile = value; } }
        [SerializeField] private TileType _hallwayTile = TileType.Wall_NA;

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