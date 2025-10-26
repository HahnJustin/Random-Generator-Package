using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Dalichrome.RandomGenerator.Configs
{
    public class ChanceFilterConfig : AbstractRegionFilterConfig
    {
        public override string IconName => "chancefilter-icon";

        public ChanceFilterConfig()
        {
            _description = StringType.Description_Filter_Chance;
        }

        public override FilterType Type { get { return FilterType.Chance; } }

        public float Probability { get { return _probability; } set { _probability = value; } }
        [SerializeField] private float _probability = 0.5f;

    }
}