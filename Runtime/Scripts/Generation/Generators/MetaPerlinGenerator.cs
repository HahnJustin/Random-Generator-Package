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
            int newNoise = random.NextInt(100000);

            float xOrg = width / 2f;
            float yOrg = height / 2f;
            Vector2 origin = new(xOrg, yOrg);

            // Cache field key once (fast + Burst-friendly storage type)
            FixedString64Bytes field = new FixedString64Bytes(config.MetaKey);

            float cutoff = Mathf.Max(0.000001f, config.Cutoff); // avoid divide-by-zero

            for (float y = 0.0f; y < height; y++)
            {
                for (float x = 0.0f; x < width; x++)
                {
                    float xCoord = xOrg + x / width * config.Scale;
                    float yCoord = yOrg + y / height * config.Scale;

                    float sample = Mathf.PerlinNoise(xCoord + newNoise, yCoord + newNoise); // 0..1

                    if (config.OvalFade)
                    {
                        Vector2 current = new Vector2(x, y);
                        Vector2 direction = current - origin;
                        float degree = Vector2.Angle(direction, Vector2.up);

                        float radius =
                            (xOrg * yOrg) /
                            Mathf.Sqrt(
                                (Mathf.Pow(xOrg, 2) * Mathf.Pow(Mathf.Sin(degree), 2)) +
                                (Mathf.Pow(yOrg, 2) * Mathf.Pow(Mathf.Cos(degree), 2))
                            );

                        float distance = Vector2.Distance(current, origin);
                        sample += distance * config.OvalScale / radius;
                    }

                    // Match your PerlinGenerator behavior: only "apply" when under cutoff.
                    if (sample < config.Cutoff)
                    {
                        // Normalize to 0..1 within the cutoff band, like your tile index logic does.
                        float normalized = Mathf.Clamp01(sample / cutoff);

                        int value = Mathf.RoundToInt(normalized * config.MaxValue);
                        value = Mathf.Clamp(value, 0, config.MaxValue);

                        int2 pos = new int2(Mathf.FloorToInt(x), Mathf.FloorToInt(y));
                        TileGrid.AddData(pos, field, value);
                    }
                }
            }

            return input;
        }
    }
}