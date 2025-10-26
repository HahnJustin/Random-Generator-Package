using Dalichrome.RandomGenerator.Core;
using Dalichrome.RandomGenerator.Data;
using UnityEngine;

namespace Dalichrome.RandomGenerator.Generators
{
    public interface IFilter : IAbstractOperation
    {
        public Generation Do(RegionSplits regionSplits);

        public bool Filter(RegionBounds bounds);

        public void SubInitialize(RegionSplits splits);
    }
}
