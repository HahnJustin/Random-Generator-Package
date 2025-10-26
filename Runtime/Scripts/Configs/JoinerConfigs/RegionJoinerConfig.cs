using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Dalichrome.RandomGenerator.Configs
{
    public class RegionJoinerConfig : AbstractRegionJoinerConfig
    {
        public override string IconName => "regionjoiner-icon";

        public RegionJoinerConfig()
        {
            _description = StringType.Description_Joiner_RegionJoin;
        }

        public override JoinerType Type { get { return JoinerType.RegionJoin; } }

    }
}