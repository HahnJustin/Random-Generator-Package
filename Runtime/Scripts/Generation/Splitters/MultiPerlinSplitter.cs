using System.Collections.Generic;
using System.Linq;
using Dalichrome.RandomGenerator.Configs;
using Dalichrome.RandomGenerator.Core;
using Dalichrome.RandomGenerator.Data;
using Unity.Mathematics;
using UnityEngine;

namespace Dalichrome.RandomGenerator.Generators
{
    public class MultiPerlinSplitter : AbstractSplitter<MultiPerlinSplitterConfig>
    {
        public MultiPerlinSplitter(MultiPerlinSplitterConfig config, int outputs) : base(config, outputs) { }

        protected override RegionSplits Split(Generation generation)
        {
            RegionBounds bounds = generation.Grid.GetRegionBounds();
            RegionSplits regionSplits = new(generation);

            int bucketCount = config.BucketCount;

            float[] frequencies = new float[]
            {
                config.FrequencyA,
                config.FrequencyB,
                config.FrequencyC
            };

            float2[] offsets = new float2[frequencies.Length];
            if (config.UseRandomOffsets)
            {
                for (int i = 0; i < frequencies.Length; i++)
                    offsets[i] = new float2(random.NextFloat(0f, 1000f), random.NextFloat(0f, 1000f));
            }

            Dictionary<int, List<int2>> regionBuckets = new();

            foreach (Tile tile in generation.Grid)
            {
                float[] noiseVals = new float[frequencies.Length];
                for (int i = 0; i < frequencies.Length; i++)
                {
                    float2 sample = (float2)tile.Int2 * frequencies[i];
                    if (config.UseRandomOffsets)
                        sample += offsets[i];

                    noiseVals[i] = Mathf.PerlinNoise(sample.x, sample.y);
                }

                int regionKey = GetRegionKey(noiseVals, bucketCount, config.FieldWeights);

                if (!regionBuckets.TryGetValue(regionKey, out var list))
                {
                    list = new List<int2>();
                    regionBuckets[regionKey] = list;
                }

                list.Add(tile.Int2);
            }

            // Create RegionBounds
            foreach (var kvp in regionBuckets)
            {
                List<int2> group = kvp.Value;
                if (group.Count == 0) continue;

                int2 min = group[0];
                int2 max = group[0];
                foreach (var p in group)
                {
                    min = math.min(min, p);
                    max = math.max(max, p);
                }

                regionSplits.AddRegion(new RegionBounds(min, max, group));
            }

            return regionSplits;
        }

        private int GetRegionKey(float[] noiseVals, int bucketCount, Vector3 weights)
        {
            int key = 0;
            int baseMultiplier = 1;

            for (int i = 0; i < noiseVals.Length; i++)
            {
                float weightedNoise = noiseVals[i] * weights[i]; // Apply weight
                float clamped = Mathf.Clamp01(weightedNoise); // Clamp to avoid overflow
                int bucket = Mathf.FloorToInt(clamped * bucketCount);
                bucket = Mathf.Clamp(bucket, 0, bucketCount - 1);

                key += bucket * baseMultiplier;
                baseMultiplier *= bucketCount;
            }

            return key;
        }
    }
}
