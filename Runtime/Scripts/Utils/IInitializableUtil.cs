using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Dalichrome.RandomGenerator.Utils
{
    public interface IInitializableUtil
    {
        public bool DoInitialization { get; set; }
        public abstract void Initialize();
    }
}