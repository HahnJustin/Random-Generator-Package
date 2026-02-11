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

            FixedString64Bytes field = new (config.MetaKey);

            // Precompute to avoid divides in the inner loop
            float xMult = config.ScaleWithMapSize ? 1f / width : 0.01f;
            float yMult = config.ScaleWithMapSize ? 1f / height : 0.01f;

            for (int y = 0; y < height; y++)
            {
                float ny = (y * yMult) * config.Scale + offY;

                for (int x = 0; x < width; x++)
                {
                    float nx = (x * xMult) * config.Scale + offX;

                    TileGrid.AddData(new int2(x, y), field, Mathf.PerlinNoise(nx, ny));
                }
            }

            return input;
        }
    }
}