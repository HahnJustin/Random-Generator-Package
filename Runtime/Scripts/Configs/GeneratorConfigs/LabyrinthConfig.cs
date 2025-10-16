using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;
using Dalichrome.RandomGenerator.Core;

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

        [TileDisplay] public int WallTile { get { return _wallTile; } set { _wallTile = value; } }
        [TileDisplay, SerializeField] private int _wallTile = (int)TileDefaults.Wall_Cave;

        [TileDisplay] public int HallwayTile { get { return _hallwayTile; } set { _hallwayTile = value; } }
        [TileDisplay, SerializeField] private int _hallwayTile = (int)TileDefaults.Wall_NA;

        public OccupanceType Occupance { get { return _occupance; } set { _occupance = value; } }
        [SerializeField] protected OccupanceType _occupance = OccupanceType.Default;

        [LayerDisplay, Condition("Occupance", OccupanceType.Layer_Not_NA)] public int OccupyLayer { get { return _occupyLayer; } set { _occupyLayer = value; } }
        [LayerDisplay, Condition("Occupance", OccupanceType.Layer_Not_NA), SerializeField] protected int _occupyLayer = (int)LayerType.Wall;

        [TileDisplay, Condition("Occupance", OccupanceType.Contains_A)] public int TileA { get { return _tileA; } set { _tileA = value; } }
        [TileDisplay, Condition("Occupance", OccupanceType.Contains_A), SerializeField] protected int _tileA = (int)TileDefaults.Wall_Cave;

        public bool InvertOccupance { get { return _invertOccupance; } set { _invertOccupance = value; } }
        [SerializeField] protected bool _invertOccupance = false;

        public bool ConfigureFillTile => false;

        public int FillTile { get { return 0; } set { } }
    }
}