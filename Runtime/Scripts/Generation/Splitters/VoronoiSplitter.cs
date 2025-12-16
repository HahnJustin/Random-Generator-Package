using Dalichrome.RandomGenerator.Configs;
using Dalichrome.RandomGenerator.Utils;
using Dalichrome.RandomGenerator.Data;
using System.Collections.Generic;
using Dalichrome.RandomGenerator.Core;
using Unity.Mathematics;
using Dalichrome.RandomGenerator.Random;
using System.Linq;
using UnityEngine;

namespace Dalichrome.RandomGenerator.Generators
{
    public class VoronoiSplitter : AbstractSplitter<VoronoiSplitterConfig>
    {
        public VoronoiSplitter(VoronoiSplitterConfig config, int outputs) : base(config, outputs) { }

        private static List<int2> GenerateRandomSeeds(
            Generation generation,
            int count,
            AbstractRandom rng)
        {
            var seeds = new HashSet<int2>();
            RegionBounds bounds = generation.Grid.GetRegionBounds();
            List<int2> sourcePositions = bounds.includingPositions;

            if (sourcePositions != null && sourcePositions.Count > 0)
            {
                int total = sourcePositions.Count;
                while (seeds.Count < count && seeds.Count < total)
                {
                    var pos = sourcePositions[rng.NextInt(total)];
                    seeds.Add(pos);
                }

                return seeds.ToList();
            }

            // Fallback: generate from full bounds
            int width = generation.Width;
            int height = generation.Height;
            int2 min = generation.Minimum;

            while (seeds.Count < count)
            {
                int x = rng.NextInt(width) + min.x;
                int y = rng.NextInt(height) + min.y;
                seeds.Add(new int2(x, y));
            }

            return seeds.ToList();
        }

        private int FindNearestSeedStep(List<int2> seeds, int2 pos)
        {
            int closestSeed = -1;
            float closestDistSq = float.MaxValue;

            for (int i = 0; i < seeds.Count; i++)
            {
                float distSq = math.distancesq(pos, seeds[i]);
                if (distSq < closestDistSq)
                {
                    closestDistSq = distSq;
                    closestSeed = i;
                }
            }

            return closestSeed;
        }

        protected override RegionSplits Split(Generation generation)
        {
            RegionBounds bounds = generation.Grid.GetRegionBounds();
            RegionSplits regionSplits = new(generation);

            List<int2> regionPositions = bounds.includingPositions;

            // Generate Voronoi seeds from within the region
            List<int2> seeds = GenerateRandomSeeds(generation, random.NextInt(config.RegionMin, config.RegionMax), random);

            // Execute seed buckets
            Dictionary<int, List<int2>> regionBuckets = new();
            for (int i = 0; i < seeds.Count; i++)
                regionBuckets[i] = new List<int2>();

            // Assign each tile in the region to the nearest seed
            foreach (int2 pos in generation.Grid.GetPositions())
            {
                int closestSeed = FindNearestSeedStep(seeds, pos);
                regionBuckets[closestSeed].Add(pos);
            }
            
            // Create RegionBounds from each cluster
            foreach (var kvp in regionBuckets)
            {
                var positions = kvp.Value;
                if (positions.Count == 0) continue;

                int2 regionMin = positions[0];
                int2 regionMax = positions[0];

                foreach (var p in positions)
                {
                    regionMin = math.min(regionMin, p);
                    regionMax = math.max(regionMax, p);
                }

                regionSplits.AddRegion(new RegionBounds(regionMin, regionMax, positions, TileGrid));
            }

            return regionSplits;
        }
    }
}
