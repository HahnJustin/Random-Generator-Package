using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Dalichrome.RandomGenerator.Configs
{
    [Serializable]
    public class RandomWalkConfig : AbstractGeneratorConfig
    {
        public RandomWalkConfig()
        {
            _description = StringType.Description_Generator_RandomWalk;
        }

        public override GeneratorType Type { get { return GeneratorType.Random_Walk; } }

        public int Steps { get { return _steps; } set { _steps = value; } }
        [SerializeField] private int _steps = 100;

        public float OriginRange { get { return _originRange; } set { _originRange = value; } }
        [SerializeField] private float _originRange = 8;

        public TileType Path { get { return _path; } set { _path = value; } }
        [SerializeField] private TileType _path = TileType.Wall_NA;

        public bool DebugEnds { get { return _debugEnds; } set { _debugEnds = value; } }
        [SerializeField] private bool _debugEnds = false;
    }
}