using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Dalichrome.RandomGenerator.Configs
{
    public abstract class AbstractLogicFilterConfig : AbstractRegionFilterConfig, IJoinConfig
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

        protected int _inputCount = 1;
        [SerializeField, Hidden] public int InputCount { get { return _inputCount; } set { _inputCount = value; } }
    }
}