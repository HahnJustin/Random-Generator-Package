using Dalichrome.RandomGenerator.Core;
using System;
using System.Collections.Generic;
using UnityEngine;


namespace Dalichrome.RandomGenerator.Configs
{
    [Serializable]
    public abstract class AbstractGeneratorConfig : AbstractMaskedOperationConfig
    {
        public override string IconName => "generator-icon";

        [Hidden] public new virtual GeneratorType Type { get; }

        public override string ToString()
        {
            return Type.ToString() + " Generator";
        }
    }
}
