using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace Dalichrome.RandomGenerator.Core
{
    public class RegionBounds
    {
        public List<int2> includingPositions;
        public int2 min;
        public int2 max;

        public int Size => includingPositions?.Count ?? 0;

        public RegionBounds(int2 minimum, int2 maximum, List<int2> regionIncludedPositons)
        {
            includingPositions = regionIncludedPositons;

            min = minimum;
            max = maximum;
        }

        public RegionBounds(BoundsInt bounds, List<int2> regionIncludedPositons) :
            this(new(bounds.min.x, bounds.min.y), new(bounds.max.x, bounds.max.y), regionIncludedPositons)
        { }

        public static IComparer<RegionBounds> SizeComparerAscending { get; } = new AscendingSizeComparer();
        public static IComparer<RegionBounds> SizeComparerDescending { get; } = new DescendingSizeComparer();

        private class AscendingSizeComparer : IComparer<RegionBounds>
        {
            public int Compare(RegionBounds a, RegionBounds b)
            {
                return a.Size.CompareTo(b.Size);
            }
        }

        private class DescendingSizeComparer : IComparer<RegionBounds>
        {
            public int Compare(RegionBounds a, RegionBounds b)
            {
                return b.Size.CompareTo(a.Size);
            }
        }

        public void AddRegion(RegionBounds bounds)
        {
            if (bounds == null || bounds.includingPositions == null)
                return;

            // Use a HashSet for fast deduplication
            HashSet<int2> existing = new(includingPositions);
            foreach (var pos in bounds.includingPositions)
            {
                if (existing.Add(pos))  // Only add if not already in set
                    includingPositions.Add(pos);
            }

            // Expand bounds
            min = math.min(min, bounds.min);
            max = math.max(max, bounds.max);
        }

    }
}
