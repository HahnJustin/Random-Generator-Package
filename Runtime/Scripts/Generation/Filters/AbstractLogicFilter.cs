using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using Dalichrome.RandomGenerator.Configs;
using Dalichrome.RandomGenerator.Random;
using Dalichrome.RandomGenerator.Core;
using System.Threading.Tasks;
using Dalichrome.RandomGenerator.Data;

namespace Dalichrome.RandomGenerator.Generators
{
    public abstract class AbstractLogicFilter<C> : AbstractFilter<C>, ILogicFilter
        where C : AbstractLogicFilterConfig
    {
        protected List<IFilter> filters = new ();

        protected AbstractLogicFilter(C config, List<IFilter> filters) : base(config)
        {
            this.filters = filters;
        }

        //Initializes all subfilters
        public override void SubInitialize(RegionSplits splits)
        {
            foreach (var filter in filters) filter.SubInitialize(splits);
        }

        protected override void Initialize(RegionSplits splits)
        {
            base.Initialize(splits);
            SubInitialize(splits);
        }
    }
}
