using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Dalichrome.RandomGenerator.Configs
{
    public class GuaranteeFilterConfig : AbstractRegionFilterConfig
    {
        public override string IconName => "guaranteefilter-icon";

        public GuaranteeFilterConfig()
        {
            _description = StringType.Description_Filter_Guarantee;
        }

        public override FilterType Type { get { return FilterType.Guarantee; } }
    }
}