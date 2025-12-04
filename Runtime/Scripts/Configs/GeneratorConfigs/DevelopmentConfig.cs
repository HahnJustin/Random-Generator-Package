using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;
using Dalichrome.RandomGenerator.Core;

namespace Dalichrome.RandomGenerator.Configs
{
    [Serializable]
    public class DevelopmentConfig : AbstractGeneratorConfig, IOccupanceConfig
    {
        public DevelopmentConfig()
        {
            _description = StringType.Description_Generator_Development;
        }

        public override GeneratorType Type { get { return GeneratorType.In_Development; } }

        public string MetaFieldName { get { return _metaFieldName; } set { _metaFieldName = value; } }
        [SerializeField] private string _metaFieldName = "";

        public float Density { get { return _density; } set { _density = value; } }
        [SerializeField] private float _density = 0.5f;

        public OccupanceType Occupance { get { return _occupance; } set { _occupance = value; } }
        [SerializeField] protected OccupanceType _occupance = OccupanceType.Contains_A;

        [LayerDisplay, Condition("Occupance", OccupanceType.Layer_Not_NA)] public int OccupyLayer { get { return _occupyLayer; } set { _occupyLayer = value; } }
        [LayerDisplay, Condition("Occupance", OccupanceType.Layer_Not_NA), SerializeField] protected int _occupyLayer = (int)LayerType.Wall;

        [TileDisplay, Condition("Occupance", OccupanceType.Contains_A)] public int TileA { get { return _tileA; } set { _tileA = value; } }
        [TileDisplay, Condition("Occupance", OccupanceType.Contains_A), SerializeField] protected int _tileA = (int)TileDefaults.Wall_NA;

        public bool InvertOccupance { get { return _invertOccupance; } set { _invertOccupance = value; } }
        [SerializeField] protected bool _invertOccupance = false;
    }
}