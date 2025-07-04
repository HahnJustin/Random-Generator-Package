using Dalichrome.RandomGenerator.Configs;
using Dalichrome.RandomGenerator.Data;

namespace Dalichrome.RandomGenerator.Generators
{
    public class BorderGenerator : AbstractGenerator<BorderConfig>
    {
        public BorderGenerator(BorderConfig config) : base(config) { }

        protected override Generation Enact(Generation input)
        {
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if(x < config.Depth || width - x < config.Depth ||
                       y < config.Depth || height - y < config.Depth)
                    {
                        TileGrid.SetTileId(x, y, config.Border);
                    }
                }
            }
            return input;
        }
    }
}
