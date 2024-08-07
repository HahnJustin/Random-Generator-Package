using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Dalichrome.RandomGenerator.Configs
{
    [Serializable]
    public class UpscaledNoiseConfig : AbstractGeneratorConfig
    {
        public UpscaledNoiseConfig()
        {
            _description = StringType.Description_Generator_UpscaledNoise;
        }

        public override GeneratorType Type { get { return GeneratorType.Upscaled_Noise; } }

        public float BaseNoiseRatio { get { return _baseNoiseRatio; } set { _baseNoiseRatio = value; } }
        [SerializeField] private float _baseNoiseRatio = 0.0625f;

        public float Density { get { return _density; } set { _density = value; } }
        [SerializeField] private float _density = 0.5f;

        public bool ForceDensity { get { return _forceDensity; } set { _forceDensity = value; } }
        [SerializeField] private bool _forceDensity = false;

        public float ForcePower { get { return _forcePower; } set { _forcePower = value; } }
        [SerializeField] private float _forcePower = 1f;

        public float LastGridImpact { get { return _lastGridImpact; } set { _lastGridImpact = value; } }
        [SerializeField] private float _lastGridImpact = 0.15f;

        public bool WrapBounds { get { return _wrapBounds; } set { _wrapBounds = value; } }
        [SerializeField] private bool _wrapBounds = false;

        [Condition("WrapBounds", false)] public bool OutOfBoundsOccupied { get { return _occupancy; } set { _occupancy = value; } }
        [SerializeField] private bool _occupancy = false;

        public TileType FillTile { get { return _fillTile; } set { _fillTile = value; } }
        [SerializeField] private TileType _fillTile = TileType.Wall_Cave;

    }
}
