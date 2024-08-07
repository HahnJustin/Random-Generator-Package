using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
        public List<TileType> AddToUniversalMaskTiles { get { return _addToUniversalMaskTIles; } set { _addToUniversalMaskTIles = value; } }
        [SerializeField] private List<TileType> _addToUniversalMaskTIles = new() { TileType.Wall_Cave };
    }
}