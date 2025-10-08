using Dalichrome.RandomGenerator.Core;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Dalichrome.RandomGenerator.Configs
{
    public class ContainsFilterConfig : AbstractRegionFilterConfig
    {
        public override string IconName => "containsfilter-icon";

        public ContainsFilterConfig()
        {
            _description = StringType.Description_Filter_Contains;
        }

        public override FilterType Type { get { return FilterType.Chance; } }

        [TileDisplay] public int TileToCheck { get { return _tileToCheck; } set { _tileToCheck = value; } }
        [TileDisplay, SerializeField] protected int _tileToCheck = (int)TileDefaults.Object_Ore_Iron;

        public int Amount { get { return _amount; } set { _amount = value; } }
        [SerializeField] protected int _amount = 1;

        public CompareEvaluationType EvaluationMetric { get { return _metric; } set { _metric = value; } }
        [SerializeField] protected CompareEvaluationType _metric = CompareEvaluationType.MoreThanEqual;
    }
}