using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Dalichrome.RandomGenerator.Core;

namespace Dalichrome.RandomGenerator.Configs
{
    [Serializable]
    public class PerlinConfig : AbstractGeneratorConfig
    {
        public PerlinConfig()
        {
            _description = StringType.Description_Generator_Perlin;
        }

        public override GeneratorType Type { get { return GeneratorType.Perlin; } }

        public float Scale { get { return _scale; } set { _scale = value; } }
        [SerializeField] private float _scale = 5f;

        public float Cutoff { get { return _cutoff; } set { _cutoff = value; } }
        [SerializeField] private float _cutoff = 0.5f;

        public bool OvalFade { get { return _ovalFade; } set { _ovalFade = value; } }
        [SerializeField] private bool _ovalFade = false;

        public float OvalScale { get { return _ovalScale; } set { _ovalScale = value; } }
        [SerializeField] private float _ovalScale = 0.5f;

        [TileDisplay] public List<int> Tiles { get { return _tiles; } set { _tiles = value; } }
        [TileDisplay, SerializeField] private List<int> _tiles = new() { (int)TileDefaults.Wall_Cave };
    }
}