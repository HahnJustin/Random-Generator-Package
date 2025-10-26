using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Dalichrome.RandomGenerator.Core;

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

        [TileDisplay] public int Path { get { return _path; } set { _path = value; } }
        [TileDisplay, SerializeField] private int _path = (int)TileDefaults.Wall_NA;

        public bool DebugEnds { get { return _debugEnds; } set { _debugEnds = value; } }
        [SerializeField] private bool _debugEnds = false;
    }
}