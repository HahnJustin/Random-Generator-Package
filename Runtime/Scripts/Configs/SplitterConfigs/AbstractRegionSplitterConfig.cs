using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Dalichrome.RandomGenerator.Configs
{
    public abstract class AbstractRegionSplitterConfig : AbstractConfig
    {
        [Hidden] public new virtual SplitterType Type { get; }

        public override string ToString()
        {
            return Type.ToString() + " Splitter";
        }
    }
}