using Dalichrome.RandomGenerator.Configs;
using Dalichrome.RandomGenerator.Core;
using Dalichrome.RandomGenerator.Data;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace Dalichrome.RandomGenerator.Generators
{
    public class MetaPerlinGenerator : AbstractGenerator<MetaPerlinConfig>
    {
        public MetaPerlinGenerator(MetaPerlinConfig config) : base(config) { }

        protected override Generation Enact(Generation input)
        {
            // Small float offsets keep noise stable & avoid precision weirdness
            float offX = random.NextFloat(0f, 1000f);
            float offY = random.NextFloat(0f, 1000f);

            FixedString64Bytes field = new FixedString64Bytes(config.MetaKey);

            // Precompute to avoid divides in the inner loop
            float invW = 1f / width;
            float invH = 1f / height;

            for (int y = 0; y < height; y++)
            {
                float ny = (y * invH) * config.Scale + offY;

                for (int x = 0; x < width; x++)
                {
                    float nx = (x * invW) * config.Scale + offX;

                    float sample = Mathf.PerlinNoise(nx, ny); // 0..1

                    int value = Mathf.RoundToInt(sample * config.MaxValue);
                    value = Mathf.Clamp(value, 0, config.MaxValue);

                    TileGrid.AddData(new int2(x, y), field, value);
                }
            }

            return input;
        }
    }
}