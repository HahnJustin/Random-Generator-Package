using Dalichrome.RandomGenerator.Configs;
using Dalichrome.RandomGenerator.Core;
using Dalichrome.RandomGenerator.Data;

namespace Dalichrome.RandomGenerator.Generators
{
    public class ChanceFilter : AbstractFilter<ChanceFilterConfig>
    {
        public ChanceFilter(ChanceFilterConfig config) : base(config) { }

        public override bool Filter(RegionBounds region)
        {
            if(random.NextFloat() <= config.Probability)
            {
                return true;
            }
            return false;
        }
    }
}
