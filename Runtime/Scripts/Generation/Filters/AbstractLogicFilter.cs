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
    public abstract class AbstractLogicFilter<C> : AbstractFilter<C>
        where C : AbstractLogicFilterConfig
    {
        protected AbstractLogicFilter(C config) : base(config) {}
    }
}
