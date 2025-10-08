using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Dalichrome.RandomGenerator.Core;

namespace Dalichrome.RandomGenerator.Configs
{
    [Serializable]
    public class DistanceFillConfig : AbstractGeneratorConfig, IDistanceConfig
    {
        public DistanceFillConfig()
        {
            _description = StringType.Description_Generator_DistanceFill;
        }

        public override GeneratorType Type { get { return GeneratorType.Distance_Fill; } }

        public int LowerDepth { get { return _lowerDepth; } set { _lowerDepth = value; } }
        [SerializeField] private int _lowerDepth = 1;

        public int UpperDepth { get { return _upperDepth; } set { _upperDepth = value; } }
        [SerializeField] private int _upperDepth = 1;

        [TileDisplay]public int FillTile { get { return _filTile; } set { _filTile = value; } }
        [TileDisplay, SerializeField] private int _filTile = (int)TileDefaults.Wall_Cave;

        public DistanceType Distance { get { return _distance; } set { _distance = value; } }
        [SerializeField] protected DistanceType _distance = DistanceType.Cardinal;

        public bool FillOccupied { get { return _fillOccupied; } set { _fillOccupied = value; } }
        [SerializeField] protected bool _fillOccupied = true;

        public OccupanceType Occupance { get { return _occupance; } set { _occupance = value; } }
        [SerializeField] protected OccupanceType _occupance = OccupanceType.Wall_Obj_Not_NA;

        [Condition("Occupance", OccupanceType.Layer_Not_NA)] public LayerType OccupyLayer { get { return _occupyLayer; } set { _occupyLayer = value; } }
        [Condition("Occupance", OccupanceType.Layer_Not_NA), SerializeField] protected LayerType _occupyLayer = LayerType.Wall;

        [TileDisplay, Condition("Occupance", OccupanceType.Contains_A)] public int TileA { get { return _tileA; } set { _tileA = value; } }
        [TileDisplay, Condition("Occupance", OccupanceType.Contains_A), SerializeField] protected int _tileA = (int)TileDefaults.Wall_NA;

        public bool InvertOccupance { get { return _invertOccupance; } set { _invertOccupance = value; } }
        [SerializeField] protected bool _invertOccupance = false;
    }
}
