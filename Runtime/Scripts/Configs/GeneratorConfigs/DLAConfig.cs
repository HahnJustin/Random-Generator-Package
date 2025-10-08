using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;
using Dalichrome.RandomGenerator.Core;

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

        [TileDisplay] public int StickTo { get { return _stickTo; } set { _stickTo = value; } }
        [TileDisplay, SerializeField] private int _stickTo = (int)TileDefaults.Wall_NA;

        public OccupanceType Occupance { get { return _occupance; } set { _occupance = value; } }
        [SerializeField] protected OccupanceType _occupance = OccupanceType.Contains_A;

        [Condition("Occupance", OccupanceType.Layer_Not_NA)] public LayerType OccupyLayer { get { return _occupyLayer; } set { _occupyLayer = value; } }
        [Condition("Occupance", OccupanceType.Layer_Not_NA), SerializeField] protected LayerType _occupyLayer = LayerType.Wall;

        [TileDisplay, Condition("Occupance", OccupanceType.Contains_A)] public int TileA { get { return _tileA; } set { _tileA = value; } }
        [TileDisplay, Condition("Occupance", OccupanceType.Contains_A), SerializeField] protected int _tileA = (int)TileDefaults.Wall_NA;

        public bool InvertOccupance { get { return _invertOccupance; } set { _invertOccupance = value; } }
        [SerializeField] protected bool _invertOccupance = false;
    }
}