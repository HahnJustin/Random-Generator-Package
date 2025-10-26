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
            foreach (int2 pos in TileGrid.GetPositions())
            {
                if (TileGrid.ColumnContainsId(pos, config.TileToCheck)) return pos;
            }
            return CANNOT_FIND_TILE_POS;
        }

        public override bool Filter(RegionBounds region)
        {
            if (math.all(mainPosition == CANNOT_FIND_TILE_POS)) return false;

            foreach (int2 pos in region)
            {
                int2 delta = pos - mainPosition;
                int distSq = delta.x * delta.x + delta.y * delta.y;

                int minSq = config.MinDistance * config.MinDistance;
                int maxSq = config.MaxDistance * config.MaxDistance;

                if (distSq <= maxSq && distSq >= minSq)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
