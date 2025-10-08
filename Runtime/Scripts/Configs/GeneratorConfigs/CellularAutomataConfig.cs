using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using Dalichrome.RandomGenerator.Core;
using UnityEngine;

namespace Dalichrome.RandomGenerator.Configs
{
    [Serializable]
    public class CellularAutomataConfig : AbstractGeneratorConfig, IOccupanceConfig
    {
        public CellularAutomataConfig()
        {
            _description = StringType.Description_Generator_CellularA;
        }

        public override GeneratorType Type { get { return GeneratorType.Cellular_Automata; } }

        public int LiveNeighboursRequired { get { return _liveNeighboursRequired; } set { _liveNeighboursRequired = value; } }
        [SerializeField] private int _liveNeighboursRequired = 5;

        public int Repetitions { get { return _repetitions; } set { _repetitions = value; } }
        [SerializeField] private int _repetitions = 2;

        [TileDisplay] public int Fill { get { return _fill; } set { _fill = value; } }
        [TileDisplay, SerializeField] private int _fill = (int)TileDefaults.Wall_Cave;

        [TileDisplay] public int Empty { get { return _empty; } set { _empty = value; } }
        [TileDisplay, SerializeField] private int _empty = (int)TileDefaults.Wall_NA;

        public float PlaceProbability { get { return _placeProbability; } set { _placeProbability = value; } }
        [SerializeField] private float _placeProbability = 1f;

        public bool BorderOccupied { get { return _borderOccupied; } set { _borderOccupied = value; } }
        [SerializeField] private bool _borderOccupied = true;

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