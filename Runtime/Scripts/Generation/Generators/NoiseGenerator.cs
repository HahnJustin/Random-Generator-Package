using System.Collections;
using System.Collections.Generic;
using Dalichrome.RandomGenerator.Configs;

namespace Dalichrome.RandomGenerator.Generators
{
    public class NoiseGenerator : AbstractGenerator
    {
        protected new NoiseConfig config;

        public NoiseGenerator(NoiseConfig config) : base(config)
        {
            this.config = config;
        }

        protected override void Enact()
        {
            List<TileType> tileTypes = config.Tiles;
            if (tileTypes == null || tileTypes.Count == 0) return;


            for (int iteration = 0; iteration < config.Repetitions; iteration++)
            {
                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        if (random.NextFloat() < config.Density)
                            TileGrid.SetTileType(x, y, tileTypes[random.NextInt(0, tileTypes.Count)]);
                    }
                }
                CancelCheck();
            }
        }
    }
}
