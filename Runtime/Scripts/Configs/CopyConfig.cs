using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Dalichrome.RandomGenerator.Configs
{
    [Serializable]
    public class CopyConfig : AbstractGeneratorConfig
    {
        public CopyConfig()
        {
            _description = StringType.Description_Generator_Copy;
        }

        public override GeneratorType Type { get { return GeneratorType.Copy; } }

        public List<SerialPair<TileType, TileType>> FromTo { get { return _fromTo; } set { _fromTo = value; } }
        [SerializeField] public List<SerialPair<TileType, TileType>> _fromTo = new() { new(TileType.Wall_Cave, TileType.Wall_Cave_Light) };
    }
}
