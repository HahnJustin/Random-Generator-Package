using Dalichrome.RandomGenerator.Random;
using Dalichrome.RandomGenerator.Core;
using System;
using System.Threading;
using UnityEngine;
using Unity.VisualScripting.YamlDotNet.Core.Tokens;
using System.Collections.Generic;

namespace Dalichrome.RandomGenerator
{
    public class Generation : AbstractGridOperationData, IDisposable
    {
        public Generation() : base()
        {
        }

        public Generation(AbstractGridOperationData data) : base(data) { }

        public Generation(GenerationParams genParams) : base()
        {
            Grid = new(genParams.Width, genParams.Height);
            Seed = genParams.Seed;
        }
    }
}