using Dalichrome.RandomGenerator.Configs;
using Dalichrome.RandomGenerator.Core;
using Dalichrome.RandomGenerator.Data;
using Dalichrome.RandomGenerator.Generators;
using System.Collections.Generic;
using Unity.Mathematics;

namespace Dalichrome.RandomGenerator.Generators
{
    public class PerlinSplitter : AbstractSplitter<PerlinSplitterConfig>
    {
        public PerlinSplitter(PerlinSplitterConfig config, int outputs) : base(config, outputs) { }

        protected override RegionSplits Split(Generation generation)
        {
            RegionBounds bounds = generation.Grid.GetRegionBounds();
            RegionSplits regionSplits = new(generation);

            float frequency = config.Frequency;
            int numBiomes = config.RegionCount;

            // Optional offset to vary noise seed
            float2 offset = config.UseRandomOffset
                ? new float2(random.NextFloat(), random.NextFloat()) * 100f
                : float2.zero;

            // Prepare biome buckets
            Dictionary<int, List<int2>> biomeBuckets = new();
            for (int i = 0; i < numBiomes; i++)
                biomeBuckets[i] = new List<int2>();

            foreach (Tile tile in generation.Grid)
            {
                float2 samplePos = (float2)tile.Int2 * frequency + offset;
                float noiseValue = noise.cnoise(samplePos);
                float normalized = math.saturate((noiseValue + 1f) * 0.5f); // 0 to 1

                int biomeIndex = (int)(normalized * numBiomes);
                biomeIndex = math.clamp(biomeIndex, 0, numBiomes - 1);
                biomeBuckets[biomeIndex].Add(tile.Int2);
            }

            // Build RegionBounds from each bucket
            foreach (var kvp in biomeBuckets)
            {
                List<int2> regionPositions = kvp.Value;
                if (regionPositions.Count == 0) continue;

                int2 regionMin = regionPositions[0];
                int2 regionMax = regionPositions[0];
                foreach (var p in regionPositions)
                {
                    regionMin = math.min(regionMin, p);
                    regionMax = math.max(regionMax, p);
                }

                regionSplits.AddRegion(new RegionBounds(regionMin, regionMax, regionPositions, TileGrid));
            }

            return regionSplits;
        }
    }
}