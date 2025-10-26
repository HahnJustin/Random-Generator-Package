using Dalichrome.RandomGenerator.Core;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Dalichrome.RandomGenerator.Configs
{
    public class UpscaleNoiseSplitterConfig : AbstractRegionSplitterConfig
    {
        //public override string IconName => "upscalenoisesplitter-icon";

        public UpscaleNoiseSplitterConfig()
        {
            _description = StringType.Description_Splitter_UpscaleNoise;
        }

        public override SplitterType Type { get { return SplitterType.UpscaleNoise; } }

        public int RegionCount { get { return _regionCount; } set { _regionCount = value; } }
        [SerializeField] private int _regionCount = 2;

        public float BaseNoiseRatio { get { return _baseNoiseRatio; } set { _baseNoiseRatio = value; } }
        [SerializeField] private float _baseNoiseRatio = 0.0625f;

        public float Density { get { return _density; } set { _density = value; } }
        [SerializeField] private float _density = 0.5f;

        public bool ForceDensity { get { return _forceDensity; } set { _forceDensity = value; } }
        [SerializeField] private bool _forceDensity = false;

        public float LastGridImpact { get { return _lastGridImpact; } set { _lastGridImpact = value; } }
        [SerializeField] private float _lastGridImpact = 0.15f;

        public bool SplitDisconnectedRegions { get { return _splitDisconnectedRegions; } set { _splitDisconnectedRegions = value; } }
        [SerializeField] private bool _splitDisconnectedRegions = false;

        public bool MajoritySmooth { get { return _majoritySmooth; } set { _majoritySmooth = value; } }
        [SerializeField] private bool _majoritySmooth = false;

        public bool WrapBounds { get { return _wrapBounds; } set { _wrapBounds = value; } }
        [SerializeField] private bool _wrapBounds = false;

        [Condition("WrapBounds", false)] public bool OutOfBoundsOccupied { get { return _outOfBoundsOccupied; } set { _outOfBoundsOccupied = value; } }
        [Condition("WrapBounds", false), SerializeField] private bool _outOfBoundsOccupied = false;
    }
}