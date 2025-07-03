using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Dalichrome.RandomGenerator.Configs
{
    public abstract class AbstractJoinConfig : AbstractConfig, IJoinConfig
    {
        protected int _inputCount = 1;
        [SerializeField, Hidden] public int InputCount { get { return _inputCount; } set { _inputCount = value; } }
    }
}