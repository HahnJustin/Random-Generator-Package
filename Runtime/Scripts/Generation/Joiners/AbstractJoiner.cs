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
    public abstract class AbstractJoiner<C> : AbstractOperation<C, RegionSplits, Generation>, IJoiner
        where C : AbstractRegionJoinerConfig
    {
        protected AbstractJoiner(C config) : base(config)
        {
        }
    }
}
