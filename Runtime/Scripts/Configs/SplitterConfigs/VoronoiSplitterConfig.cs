using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Dalichrome.RandomGenerator.Configs
{
    public abstract class VoronoiSplitterConfig : AbstractRegionSplitterConfig
    {
        public VoronoiSplitterConfig()
        {
            _description = StringType.Description_Splitter_Voronoi;
        }

        public override SplitterType Type { get { return SplitterType.Voronoi; } }
    }
}