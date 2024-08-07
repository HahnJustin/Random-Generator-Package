using System.Collections;
using System.Collections.Generic;
using Dalichrome.RandomGenerator.Configs;
using Dalichrome.RandomGenerator.Utils;

namespace Dalichrome.RandomGenerator.Generators
{
    public class DistanceFillGenerator : AbstractGenerator
    {
        protected new DistanceFillConfig config;
        private readonly DistanceUtil util;

        public DistanceFillGenerator(DistanceFillConfig config) : base(config)
        {
            this.config = config;
            util = new(config);
            util.OutOfBoundsOccupancy = config.FillOccupied ? 1 : 0;
            AddUtil(util);
        }

        protected override void Enact()
        {
            foreach (Tile tile in TileGrid)
            {
                if(tile.Value >= config.LowerDepth && tile.Value <= config.UpperDepth) TileGrid.SetTileType(tile, config.FillTile);
                CancelCheck();
            }
        }
    }
}
