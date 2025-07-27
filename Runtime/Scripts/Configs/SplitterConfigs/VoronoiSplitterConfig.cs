using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Dalichrome.RandomGenerator.Configs
{
    public class VoronoiSplitterConfig : AbstractRegionSplitterConfig
    {
        public override string IconName => "voronoisplitter-icon";

        public VoronoiSplitterConfig()
        {
            _description = StringType.Description_Splitter_Voronoi;
        }

        public override SplitterType Type { get { return SplitterType.Voronoi; } }

        public int RegionMin { get { return _regionMin; } set { _regionMin = value; } }
        [SerializeField] private int _regionMin = 10;

        public int RegionMax { get { return _regionMax; } set { _regionMax = value; } }
        [SerializeField] private int _regionMax = 10;
    }
}