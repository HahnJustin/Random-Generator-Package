using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Dalichrome.RandomGenerator.Configs;

namespace Dalichrome.RandomGenerator.Generators
{
    public class PerlinGenerator : AbstractGenerator
    {

        protected new PerlinConfig config;

        public PerlinGenerator(PerlinConfig config) : base(config)
        {
            this.config = config;
        }

        protected override void Enact()
        {
            int newNoise = random.NextInt(100000);

            float xOrg = width / 2f;
            float yOrg = height / 2f;
            Vector2 origin = new(xOrg, yOrg);

            for (float y = 0.0F; y < height; y++)
            {
                for (float x = 0.0F; x < width; x++)
                {
                    float xCoord = xOrg + x / width * config.Scale;
                    float yCoord = yOrg + y / height * config.Scale;
                    float sample = Mathf.PerlinNoise(xCoord + newNoise, yCoord + newNoise);

                    Tile tile = TileGrid.GetTile(Mathf.FloorToInt(x), Mathf.FloorToInt(y));

                    if (config.OvalFade)
                    {
                        Vector2 current = new Vector2(x, y);
                        Vector2 direction = current - origin;
                        float degree = Vector2.Angle(direction, Vector2.up);
                        float radius = (xOrg * yOrg) / Mathf.Sqrt((Mathf.Pow(xOrg, 2) * Mathf.Pow(Mathf.Sin(degree), 2)) +
                                                                  (Mathf.Pow(yOrg, 2) * Mathf.Pow(Mathf.Cos(degree), 2)));
                        float distance = Vector2.Distance(current, origin);
                        sample += distance * config.OvalScale / radius;
                    }

                    if (sample < config.Cutoff)
                    {
                        float separation = config.Cutoff / (float)config.Tiles.Count;
                        float roughIndex = sample / separation;
                        int index = Mathf.Clamp(Mathf.RoundToInt(roughIndex), 0, config.Tiles.Count-1);
                        tile.SetType(config.Tiles[index]);
                    }
                }
            }
        }
    }
}
