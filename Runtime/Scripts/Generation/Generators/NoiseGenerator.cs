using System.Collections.Generic;
using Dalichrome.RandomGenerator.Configs;
using Dalichrome.RandomGenerator.Core;
using Dalichrome.RandomGenerator.Data;

namespace Dalichrome.RandomGenerator.Generators
{
    public class NoiseGenerator : AbstractGenerator<NoiseConfig>
    {
        public NoiseGenerator(NoiseConfig config) : base(config) { }

        protected override Generation Enact(Generation input)
        {
            List<int> tileTypes = config.Tiles;
            if (tileTypes == null || tileTypes.Count == 0) return input;


            for (int iteration = 0; iteration < config.Repetitions; iteration++)
            {
                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        if (random.NextFloat() < config.Density)
                            TileGrid.SetTileId(x, y, (int)tileTypes[random.NextInt(0, tileTypes.Count)]);
                    }
                }
                CancelCheck();
            }
            return input;
        }
    }
}
