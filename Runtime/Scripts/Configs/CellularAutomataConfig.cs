using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
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

        public TileType Fill { get { return _fill; } set { _fill = value; } }
        [SerializeField] private TileType _fill = TileType.Wall_Cave;

        public TileType Empty { get { return _empty; } set { _empty = value; } }
        [SerializeField] private TileType _empty = TileType.Wall_NA;

        public float PlaceProbability { get { return _placeProbability; } set { _placeProbability = value; } }
        [SerializeField] private float _placeProbability = 1f;

        public bool BorderOccupied { get { return _borderOccupied; } set { _borderOccupied = value; } }
        [SerializeField] private bool _borderOccupied = true;

        public OccupanceType Occupance { get { return _occupance; } set { _occupance = value; } }
        [SerializeField] protected OccupanceType _occupance = OccupanceType.Wall_Obj_Not_NA;

        [Condition("Occupance", OccupanceType.Layer_Not_NA)] public LayerType OccupyLayer { get { return _occupyLayer; } set { _occupyLayer = value; } }
        [SerializeField, ShowIf("Occupance", OccupanceType.Layer_Not_NA)] protected LayerType _occupyLayer = LayerType.Wall;

        [Condition("Occupance", OccupanceType.Contains_A)] public TileType TileA { get { return _tileA; } set { _tileA = value; } }
        [SerializeField, ShowIf("Occupance", OccupanceType.Contains_A)] protected TileType _tileA = TileType.Wall_Cave;

        public bool InvertOccupance { get { return _invertOccupance; } set { _invertOccupance = value; } }
        [SerializeField] protected bool _invertOccupance = false;
    }
}