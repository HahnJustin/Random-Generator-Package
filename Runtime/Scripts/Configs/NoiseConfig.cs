using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Dalichrome.RandomGenerator.Core;

namespace Dalichrome.RandomGenerator.Configs
{
    [Serializable]
    public class NoiseConfig : AbstractGeneratorConfig
    {
        public NoiseConfig()
        {
            _description = StringType.Description_Generator_Noise;
        }

        public override GeneratorType Type { get { return GeneratorType.Noise; } }

        public float Density { get { return _density; } set { _density = value; } }
        [SerializeField] private float _density = 0.5f;

        public int Repetitions { get { return _repetitions; } set { _repetitions = value; } }
        [SerializeField] private int _repetitions = 1;

        [TileDisplay] public List<int> Tiles { get { return _tiles; } set { _tiles = value; } }
        [SerializeField] private List<int> _tiles = new() { (int)TileType.Wall_Cave };
    }
}
