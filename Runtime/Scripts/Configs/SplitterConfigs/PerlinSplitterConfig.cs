using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Dalichrome.RandomGenerator.Configs
{
    public class PerlinSplitterConfig : AbstractRegionSplitterConfig
    {
        public PerlinSplitterConfig()
        {
            _description = StringType.Description_Splitter_Perlin;
        }

        public override SplitterType Type { get { return SplitterType.Perlin; } }

        public int RegionCount { get { return _regionCount; } set { _regionCount = value; } }
        [SerializeField] private int _regionCount = 5;

        public float Frequency { get { return _frequency; } set { _frequency = value; } }
        [SerializeField] private float _frequency = 0.05f;

        public bool UseRandomOffset { get { return _useRandomOffset; } set { _useRandomOffset = value; } }
        [SerializeField] private bool _useRandomOffset = true;
    }
}