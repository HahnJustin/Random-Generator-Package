using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using static Unity.Collections.AllocatorManager;

namespace Dalichrome.RandomGenerator.Core
{
    public class RegionBounds
    {
        public List<int2> includingPositions;
        public int2 min;
        public int2 max;

        public RegionBounds(int2 minimum, int2 maximum, List<int2> regionIncludedPositons)
        {
            includingPositions = regionIncludedPositons;

            min = minimum;
            max = maximum;
        }

        public RegionBounds(BoundsInt bounds, List<int2> regionIncludedPositons) :
            this(new(bounds.min.x, bounds.min.y), new(bounds.max.x, bounds.max.y), regionIncludedPositons)
        { }
    }
}
