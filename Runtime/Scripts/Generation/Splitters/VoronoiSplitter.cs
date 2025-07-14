using Dalichrome.RandomGenerator.Configs;
using Dalichrome.RandomGenerator.Utils;
using Dalichrome.RandomGenerator.Data;
using System.Collections.Generic;
using Codice.CM.Common;
using Dalichrome.RandomGenerator.Core;
using Unity.Mathematics;
using Dalichrome.RandomGenerator.Random;
using System.Linq;

namespace Dalichrome.RandomGenerator.Generators
{
    public class VoronoiSplitter : AbstractSplitter<VoronoiSplitterConfig>
    {
        public VoronoiSplitter(VoronoiSplitterConfig config, int outputs) : base(config, outputs) { }

        private static List<int2> GenerateRandomSeeds(int2 min, int2 max, int count, AbstractRandom rng)
        {
            var seeds = new HashSet<int2>();

            int width = max.x - min.x + 1;
            int height = max.y - min.y + 1;

            while (seeds.Count < count)
            {
                int x = rng.NextInt(width) + min.x;
                int y = rng.NextInt(height) + min.y;
                seeds.Add(new int2(x, y));
            }

            return seeds.ToList();
        }

        protected override RegionSplits Split(Generation generation)
        {
            RegionSplits regionSplits = new(generation);

            int2 min = generation.Minimum;
            int2 max = generation.Maximum;
            int width = max.x - min.x + 1;
            int height = max.y - min.y + 1;

            List<int2> seeds = GenerateRandomSeeds(min, max, random.NextInt(config.RegionMin, config.RegionMax), random);

            Dictionary<int, List<int2>> regionBuckets = new();

            for (int i = 0; i < seeds.Count; i++)
                regionBuckets[i] = new List<int2>();

            for (int y = min.y; y <= max.y; y++)
            {
                for (int x = min.x; x <= max.x; x++)
                {
                    int2 pos = new(x, y);
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

                    regionBuckets[closestSeed].Add(pos);
                }
            }

            foreach (var kvp in regionBuckets)
            {
                List<int2> positions = kvp.Value;
                if (positions.Count == 0) continue;

                int2 regionMin = positions[0];
                int2 regionMax = positions[0];

                foreach (var p in positions)
                {
                    regionMin = math.min(regionMin, p);
                    regionMax = math.max(regionMax, p);
                }

                regionSplits.AddRegion(new RegionBounds(regionMin, regionMax, positions));
            }

            return regionSplits;
        }
    }
}
