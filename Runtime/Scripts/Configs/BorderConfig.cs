using System;
using System.Collections;
using System.Collections.Generic;
using Dalichrome.RandomGenerator.Core;
using UnityEngine;

namespace Dalichrome.RandomGenerator.Configs
{
    [Serializable]
    public class BorderConfig : AbstractGeneratorConfig
    {
        public BorderConfig()
        {
            _description = StringType.Description_Generator_BorderFill;
        }

        public override GeneratorType Type { get { return GeneratorType.Border; } }


        [TileDisplay] public int Border { get { return _border; } set { _border = value; } }
        [SerializeField] private int _border = (int)TileType.Wall_Cave;

        public int Depth { get { return _depth; } set { _depth = value; } }
        [SerializeField] private int _depth = 5;
    }
}