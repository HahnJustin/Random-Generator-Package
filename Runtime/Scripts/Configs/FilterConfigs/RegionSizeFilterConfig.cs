using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Dalichrome.RandomGenerator.Configs
{
    public class RegionSizeFilterConfig : AbstractRegionFilterConfig
    {
        public RegionSizeFilterConfig()
        {
            _description = StringType.Description_Splitter_Room;
        }

        public override FilterType Type { get { return FilterType.RegionSize; } }

        public int MinimumRoomSize { get { return _minimumRoomSize; } set { _minimumRoomSize = value; } }
        [SerializeField] private int _minimumRoomSize = 1;

        public int MaximumRoomSize { get { return _maximumRoomSize; } set { _maximumRoomSize = value; } }
        [SerializeField] private int _maximumRoomSize = int.MaxValue;

        public RegionSizeFilterType RegionSizeFilter { get { return _regionSizeFilter; } set { _regionSizeFilter = value; } }
        [SerializeField] private RegionSizeFilterType _regionSizeFilter;
    }
}