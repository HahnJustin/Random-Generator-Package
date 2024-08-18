using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Dalichrome.RandomGenerator.Configs
{
    [Serializable]
    public class BorderConfig : AbstractGeneratorConfig
    {
        public BorderConfig()
        {
            _description = StringType.Description_Generator_Initial;
        }

        public override GeneratorType Type { get { return GeneratorType.Border; } }


        public TileType Border { get { return _border; } set { _border = value; } }
        [SerializeField] private TileType _border = TileType.Wall_Cave;

        public int Depth { get { return _depth; } set { _depth = value; } }
        [SerializeField] private int _depth = 5;
    }
}