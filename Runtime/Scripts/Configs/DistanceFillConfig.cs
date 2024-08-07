using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

        public TileType FillTile { get { return _filTile; } set { _filTile = value; } }
        [SerializeField] private TileType _filTile = TileType.Wall_Cave;

        public DistanceType Distance { get { return _distance; } set { _distance = value; } }
        [SerializeField] protected DistanceType _distance = DistanceType.Cardinal;

        public bool FillOccupied { get { return _fillOccupied; } set { _fillOccupied = value; } }
        [SerializeField] protected bool _fillOccupied = true;

        public OccupanceType Occupance { get { return _occupance; } set { _occupance = value; } }
        [SerializeField] protected OccupanceType _occupance = OccupanceType.Wall_Obj_Not_NA;

        [Condition("Occupance", OccupanceType.Layer_Not_NA)] public LayerType OccupyLayer { get { return _occupyLayer; } set { _occupyLayer = value; } }
        [SerializeField] protected LayerType _occupyLayer = LayerType.Wall;

        [Condition("Occupance", OccupanceType.Contains_A)] public TileType TileA { get { return _tileA; } set { _tileA = value; } }
        [SerializeField] protected TileType _tileA = TileType.Wall_NA;

        public bool InvertOccupance { get { return _invertOccupance; } set { _invertOccupance = value; } }
        [SerializeField] protected bool _invertOccupance = false;
    }
}
