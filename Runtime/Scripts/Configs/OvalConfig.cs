using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

        public TileType Interior { get { return _interior; } set { _interior = value; } }
        [SerializeField] private TileType _interior = TileType.NA;

        public TileType Exterior { get { return _exterior; } set { _exterior = value; } }
        [SerializeField] private TileType _exterior = TileType.Wall_Cave;
    }
}