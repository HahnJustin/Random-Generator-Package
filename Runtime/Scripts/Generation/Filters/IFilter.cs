using Dalichrome.RandomGenerator.Core;
using Dalichrome.RandomGenerator.Data;

namespace Dalichrome.RandomGenerator.Generators
{
    public interface IFilter : IAbstractOperation
    {
        public Generation Do(RegionSplits regionSplits);

        public bool Filter(RegionBounds bounds);
    }
}
