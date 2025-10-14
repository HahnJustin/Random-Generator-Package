using Dalichrome.RandomGenerator.Configs;
using Dalichrome.RandomGenerator.Utils;
using Dalichrome.RandomGenerator.Core;
using Dalichrome.RandomGenerator.Data;
using Unity.Mathematics;

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
            foreach (ITileColumn column in TileGrid)
            {
                int value = TileGrid.GetTileValue(column.Int2);
                if (value >= config.LowerDepth && value <= config.UpperDepth) TileGrid.SetTileId(column.Int2, (int)config.FillTile);
                CancelCheck();
            }
            return input;
        }
    }
}
