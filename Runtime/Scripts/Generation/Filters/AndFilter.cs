using Dalichrome.RandomGenerator.Configs;
using Dalichrome.RandomGenerator.Core;
using Dalichrome.RandomGenerator.Data;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Dalichrome.RandomGenerator.Generators
{
    public class AndFilter : AbstractLogicFilter<AndFilterConfig>
    {
        public AndFilter(AndFilterConfig config, List<IFilter> filters) : base(config, filters) { }

        public override bool Filter(RegionBounds regionBounds)
        {
            bool use = true;
            foreach (IFilter filter in filters)
            {
                use = use && filter.Filter(regionBounds);
            }
            return use;
        }
    }
}
