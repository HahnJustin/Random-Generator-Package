using Dalichrome.RandomGenerator.Configs;
using Dalichrome.RandomGenerator.Core;
using Dalichrome.RandomGenerator.Data;

namespace Dalichrome.RandomGenerator.Generators
{
    public class RandomFilter : AbstractFilter<RandomFilterConfig>
    {
        public RandomFilter(RandomFilterConfig config) : base(config) { }

        public override bool Filter(RegionBounds region)
        {
            if(config.RandomType == RandomFilterType.Guarantee || 
                (config.RandomType == RandomFilterType.Probability && random.NextFloat() <= config.Probability))
            {
                return true;
            }
            return false;
        }
    }
}
