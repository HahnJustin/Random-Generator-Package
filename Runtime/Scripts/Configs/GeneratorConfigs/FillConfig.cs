using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Dalichrome.RandomGenerator.Core;

namespace Dalichrome.RandomGenerator.Configs
{
    [Serializable]
    public class FillConfig : AbstractGeneratorConfig
    {
        public FillConfig()
        {
            _description = StringType.Description_Generator_Fill;
        }

        public override GeneratorType Type { get { return GeneratorType.Fill; } }

        public bool UseMetaProbability { get { return _useMetaProbability; } set { _useMetaProbability = value; } }
        [SerializeField] private bool _useMetaProbability;

        [Condition("UseMetaProbability", true)] public string MetaKey { get { return _metaKey; } set { _metaKey = value; } }
        [Condition("UseMetaProbability", true), SerializeField] private string _metaKey;

        [TileDisplay] public List<int> Tiles { get { return _tiles; } set { _tiles = value; } }
        [TileDisplay, SerializeField] private List<int> _tiles = new() { (int)TileDefaults.Wall_Cave };
    }
}
