using Dalichrome.RandomGenerator.Configs;
using Dalichrome.RandomGenerator.Core;
using Dalichrome.RandomGenerator.Data;

namespace Dalichrome.RandomGenerator.Generators
{
    public class RandomFilter : AbstractFilter<RandomFilterConfig>
    {
        public RandomFilter(RandomFilterConfig config) : base(config) { }

        protected override void Initialize(RegionSplits regionSplits) { }

        //TODO add more configurable options
        public override bool Filter(RegionBounds region)
        {
            return true;
        }

        protected override void PostEnact(Generation generation) { }
    }
}
