using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Dalichrome.RandomGenerator.Configs
{
    public class RandomFilterConfig : AbstractRegionFilterConfig
    {
        public RandomFilterConfig()
        {
            _description = StringType.Description_Filter_Random;
        }

        public override FilterType Type { get { return FilterType.Random; } }
    }
}