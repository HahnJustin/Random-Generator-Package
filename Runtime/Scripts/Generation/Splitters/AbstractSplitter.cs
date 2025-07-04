using Dalichrome.RandomGenerator.Configs;
using Dalichrome.RandomGenerator.Data;

namespace Dalichrome.RandomGenerator.Generators
{
    public abstract class AbstractSplitter<C> : AbstractOperation<C, Generation, RegionSplits>, ISplitter
        where C : AbstractRegionSplitterConfig
    {
        protected AbstractSplitter(C config) : base(config)
        {
        }
    }
}
