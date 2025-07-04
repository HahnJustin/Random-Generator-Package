using Dalichrome.RandomGenerator.Configs;
using Dalichrome.RandomGenerator.Utils;
using Dalichrome.RandomGenerator.Core;
using Dalichrome.RandomGenerator.Data;

namespace Dalichrome.RandomGenerator.Generators
{
    public class DistanceFillGenerator : AbstractGenerator<DistanceFillConfig>
    {
        private readonly DistanceUtil util;

        public DistanceFillGenerator(DistanceFillConfig config) : base(config)
        {
            util = new(config);
            util.OutOfBoundsOccupancy = config.FillOccupied ? 1 : 0;
            AddUtil(util);
        }

        protected override Generation Enact(Generation input)
        {
            foreach (Tile tile in TileGrid)
            {
                if(tile.Value >= config.LowerDepth && tile.Value <= config.UpperDepth) TileGrid.SetTileId(tile, (int)config.FillTile);
                CancelCheck();
            }
            return input;
        }
    }
}
