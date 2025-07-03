using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using Dalichrome.RandomGenerator.Configs;
using Dalichrome.RandomGenerator.Random;
using Dalichrome.RandomGenerator.Core;
using System.Threading.Tasks;
using Dalichrome.RandomGenerator.Utils;
using UnityEngine.Diagnostics;

namespace Dalichrome.RandomGenerator.Generators
{
    public class RandomFilter : AbstractFilter<RandomFilterConfig>
    {
        protected RandomFilter(RandomFilterConfig config) : base(config)
        {
        }

        protected override void Initialize(RegionSplits regionSplits) { }

        //TODO add more configurable options
        public override bool Filter(RegionBounds region)
        {
            return true;
        }

        protected override void PostEnact(Generation generation) { }
    }
}
