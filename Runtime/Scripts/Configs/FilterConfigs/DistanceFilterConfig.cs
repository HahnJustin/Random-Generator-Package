using Dalichrome.RandomGenerator.Core;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Dalichrome.RandomGenerator.Configs
{
    public class DistanceFilterConfig : AbstractRegionFilterConfig
    {
        //public override string IconName => "distancefilter-icon";

        public DistanceFilterConfig()
        {
            _description = StringType.Description_Filter_Distance;
        }

        public override FilterType Type { get { return FilterType.Distance; } }

        [TileDisplay] public int TileToCheck { get { return _tileToCheck; } set { _tileToCheck = value; } }
        [TileDisplay, SerializeField] protected int _tileToCheck = (int)TileType.Object_Ore_Iron;

        public int MaxDistance { get { return _maxDistance; } set { _maxDistance = value; } }
        [SerializeField] protected int _maxDistance = 1;

        public int MinDistance { get { return _minDistance; } set { _minDistance = value; } }
        [SerializeField] protected int _minDistance = 1;
    }
}