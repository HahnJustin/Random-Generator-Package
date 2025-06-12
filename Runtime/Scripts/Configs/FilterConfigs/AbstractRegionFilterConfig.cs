using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Dalichrome.RandomGenerator.Configs
{
    public abstract class AbstractRegionFilterConfig : AbstractConfig
    {
        [Hidden] public new virtual FilterType Type { get; }

        [Hidden]
        public override string Name
        {
            get
            {
                return _name;
            }
            set
            {
                _name = value;
            }
        }
    }
}