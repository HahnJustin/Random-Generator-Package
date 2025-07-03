using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using Dalichrome.RandomGenerator.Configs;
using Dalichrome.RandomGenerator.Random;
using Dalichrome.RandomGenerator.Core;
using System.Threading.Tasks;

namespace Dalichrome.RandomGenerator.Generators
{
    public abstract class AbstractFilter<C>: AbstractOperation<C, RegionSplits, Generation>, IFilter
        where C : AbstractRegionFilterConfig
    {
        protected AbstractFilter(C config) : base(config)
        {
        }

        public abstract bool Filter(RegionBounds bounds);

        protected override Generation Enact(RegionSplits regionSplits)
        {
            Generation generation = new(regionSplits);

            regionSplits.Shuffle();
            foreach (RegionBounds region in regionSplits)
            {
                if (Filter(region))
                {
                    generation.Grid.SetRegionBounds(region);
                    return generation;
                }
            }
            return null;
        }
    }
}
