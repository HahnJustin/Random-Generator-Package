using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Dalichrome.RandomGenerator.Core;

namespace Dalichrome.RandomGenerator.Configs
{
    [Serializable]
    public class UniversalMaskConfig : AbstractGeneratorConfig, IUniversalMaskConfig
    {
        public UniversalMaskConfig()
        {
            _description = StringType.Description_Generator_UniversalMask;
        }

        public override GeneratorType Type { get { return GeneratorType.Universal_Mask; } }

        [Hidden] public bool ShowUniversalMask { get { return _addToUniversalMaskTIles.Count > 0; } }
        [TileDisplay] public List<int> AddToUniversalMaskTiles { get { return _addToUniversalMaskTIles; } set { _addToUniversalMaskTIles = value; } }
        [TileDisplay, SerializeField] private List<int> _addToUniversalMaskTIles = new() { (int)TileDefaults.Wall_Cave };
    }
}