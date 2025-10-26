using Dalichrome.RandomGenerator.Configs;
using Dalichrome.RandomGenerator.Core;
using Dalichrome.RandomGenerator.Data;

namespace Dalichrome.RandomGenerator.Generators
{
    public class GuaranteeFilter : AbstractFilter<GuaranteeFilterConfig>
    {
        public GuaranteeFilter(GuaranteeFilterConfig config) : base(config) { }

        public override bool Filter(RegionBounds region)
        {
            return true;
        }
    }
}
