using Dalichrome.RandomGenerator.Configs;
using Dalichrome.RandomGenerator.Core;
using Dalichrome.RandomGenerator.Data;

namespace Dalichrome.RandomGenerator.Generators
{
    public class RegionSizeFilter : AbstractFilter<RegionSizeFilterConfig>
    {
        public RegionSizeFilter(RegionSizeFilterConfig config) : base(config) { }

        public override bool Filter(RegionBounds region)
        {
            return region.Size <= config.MaximumRoomSize && region.Size >= config.MinimumRoomSize;
        }
    }
}
