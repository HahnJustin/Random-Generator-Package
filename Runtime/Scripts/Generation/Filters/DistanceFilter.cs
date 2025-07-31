using Dalichrome.RandomGenerator.Configs;
using Dalichrome.RandomGenerator.Core;
using Dalichrome.RandomGenerator.Data;
using Unity.Mathematics;

namespace Dalichrome.RandomGenerator.Generators
{
    public class DistanceFilter : AbstractFilter<DistanceFilterConfig>
    {
        private readonly int2 CANNOT_FIND_TILE_POS = new(-1, -1);

        protected int2 mainPosition;

        public DistanceFilter(DistanceFilterConfig config) : base(config) { }

        protected override void Initialize(RegionSplits splits)
        {
            base.Initialize(splits);
            mainPosition = FindMainPosition();
        }

        private int2 FindMainPosition()
        {
            foreach (Tile tile in TileGrid.GetTiles())
            {
                if (tile.ContainsId(config.TileToCheck)) return tile.Int2;
            }
            return CANNOT_FIND_TILE_POS;
        }

        public override bool Filter(RegionBounds region)
        {
            if (math.all(mainPosition == CANNOT_FIND_TILE_POS)) return false;

            foreach (Tile tile in TileGrid)
            {
                if (math.distance(mainPosition, tile.Int2) <= config.MaxDistance && 
                    math.distance(mainPosition, tile.Int2) >= config.MinDistance)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
