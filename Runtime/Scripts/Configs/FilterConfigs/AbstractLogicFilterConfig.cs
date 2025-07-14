using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Dalichrome.RandomGenerator.Configs
{
    public abstract class AbstractLogicFilterConfig : AbstractRegionFilterConfig
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

        public override string ToString()
        {
            return Type.ToString() + " Logic Filter";
        }
    }
}