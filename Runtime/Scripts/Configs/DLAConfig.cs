using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;

namespace Dalichrome.RandomGenerator.Configs
{
    [Serializable]
    public class DLAConfig : AbstractGeneratorConfig, IOccupanceConfig
    {
        public DLAConfig()
        {
            _description = StringType.Description_Generator_DLA;
        }

        public override GeneratorType Type { get { return GeneratorType.DLA; } }

        public int MaxWalkers { get { return _maxWalkers; } set { _maxWalkers = value; } }
        [SerializeField] private int _maxWalkers = 100;

        public int Iterations { get { return _iterations; } set { _iterations = value; } }
        [SerializeField] private int _iterations = 500;

        public float Shrink { get { return _shrink; } set { _shrink = value; } }
        [SerializeField] private float _shrink = 0.995f;

        public float Radius { get { return _radius; } set { _radius = value; } }
        [SerializeField] private float _radius = 3;

        public int Speed { get { return _speed; } set { _speed = value; } }
        [SerializeField] private int _speed = 10;

        public bool HasHeadAtCenter { get { return _hasHeadAtCenter; } set { _hasHeadAtCenter = value; } }
        [SerializeField] private bool _hasHeadAtCenter = true;

        public TileType StickTo { get { return _stickTo; } set { _stickTo = value; } }
        [SerializeField] private TileType _stickTo = TileType.Wall_NA;

        public OccupanceType Occupance { get { return _occupance; } set { _occupance = value; } }
        [SerializeField] protected OccupanceType _occupance = OccupanceType.Contains_A;

        [Condition("Occupance", OccupanceType.Layer_Not_NA)] public LayerType OccupyLayer { get { return _occupyLayer; } set { _occupyLayer = value; } }
        [SerializeField] protected LayerType _occupyLayer = LayerType.Wall;

        [Condition("Occupance", OccupanceType.Contains_A)] public TileType TileA { get { return _tileA; } set { _tileA = value; } }
        [SerializeField] protected TileType _tileA = TileType.Wall_NA;

        public bool InvertOccupance { get { return _invertOccupance; } set { _invertOccupance = value; } }
        [SerializeField] protected bool _invertOccupance = false;
    }
}