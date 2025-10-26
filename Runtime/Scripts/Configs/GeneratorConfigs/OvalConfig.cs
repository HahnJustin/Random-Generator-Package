using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Dalichrome.RandomGenerator.Core;

namespace Dalichrome.RandomGenerator.Configs
{
    [Serializable]
    public class OvalConfig : AbstractGeneratorConfig
    {
        public OvalConfig()
        {
            _description = StringType.Description_Generator_Oval;
        }

        public override GeneratorType Type { get { return GeneratorType.Oval; } }

        public float Radius { get { return _radius; } set { _radius = value; } }
        [SerializeField] private float _radius = 1f;

        [TileDisplay] public int Interior { get { return _interior; } set { _interior = value; } }
        [TileDisplay, SerializeField] private int _interior = (int)TileDefaults.NA;

        [TileDisplay] public int Exterior { get { return _exterior; } set { _exterior = value; } }
        [TileDisplay, SerializeField] private int _exterior = (int)TileDefaults.Wall_Cave;
    }
}