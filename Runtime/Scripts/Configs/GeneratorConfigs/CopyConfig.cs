using System;
using System.Collections;
using System.Collections.Generic;
using Dalichrome.RandomGenerator.Core;
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

        [TilePairDisplay] public List<SerialPair<int, int>> FromTo { get { return _fromTo; } set { _fromTo = value; } }
        [TilePairDisplay, SerializeField] public List<SerialPair<int, int>> _fromTo = new() { new((int)TileDefaults.Wall_Cave, (int)TileDefaults.Wall_Cave_Light) };
    }
}
