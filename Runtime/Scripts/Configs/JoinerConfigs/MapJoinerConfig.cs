using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Dalichrome.RandomGenerator.Configs
{
    public class MapJoinerConfig : AbstractRegionJoinerConfig
    {
        public override string IconName => "mapjoiner-icon";

        public MapJoinerConfig()
        {
            _description = StringType.Description_Joiner_MapJoin;
        }

        public override JoinerType Type { get { return JoinerType.MapJoin; } }

    }
}