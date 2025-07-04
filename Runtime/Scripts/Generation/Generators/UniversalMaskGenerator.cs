using Dalichrome.RandomGenerator.Configs;
using Dalichrome.RandomGenerator.Core;
using Dalichrome.RandomGenerator.Data;

namespace Dalichrome.RandomGenerator.Generators
{
    public class UniversalMaskGenerator : AbstractGenerator<UniversalMaskConfig>
    {
        public UniversalMaskGenerator(UniversalMaskConfig config) : base(config) { }

        protected override Generation Enact(Generation input)
        {
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    Tile tile = TileGrid.GetTile(x, y);

                    foreach (TileType type in config.AddToUniversalMaskTiles)
                    {
                        if (tile.ContainsId((int)type))
                        {
                            TileGrid.AddExcludedPosition(tile.Position);
                            break;
                        }
                    }
                }
            }
            return input;
        }
    }
}
