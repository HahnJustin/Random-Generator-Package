using Dalichrome.RandomGenerator.Data;

namespace Dalichrome.RandomGenerator.Generators
{
    public interface IFilter
    {
        public Generation Do(RegionSplits regionSplits);

    }
}
