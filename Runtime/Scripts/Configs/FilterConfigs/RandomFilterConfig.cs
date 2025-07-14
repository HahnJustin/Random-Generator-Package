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

        public RandomFilterType RandomType { get { return _randomType; } set { _randomType = value; } }
        [SerializeField] private RandomFilterType _randomType;

        [Condition("RandomType", RandomFilterType.Probability)] public float Probability { get { return _probability; } set { _probability = value; } }
        [Condition("RandomType", RandomFilterType.Probability), SerializeField] private float _probability = 0.5f;

    }
}