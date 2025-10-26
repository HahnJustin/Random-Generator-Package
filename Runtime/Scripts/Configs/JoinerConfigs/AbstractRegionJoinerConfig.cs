using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Dalichrome.RandomGenerator.Configs
{
    public abstract class AbstractRegionJoinerConfig : AbstractConfig
    {
        public override string IconName => "joiner-icon";

        [Hidden] public new virtual JoinerType Type { get; }

        public override string ToString()
        {
            return Type.ToString() + " Joiner";
        }
    }
}