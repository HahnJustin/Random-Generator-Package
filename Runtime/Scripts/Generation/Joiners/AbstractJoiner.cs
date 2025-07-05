using Dalichrome.RandomGenerator.Configs;
using Dalichrome.RandomGenerator.Data;
using Dalichrome.RandomGenerator.Generators;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Dalichrome.RandomGenerator.Generators
{
    public abstract class AbstractJoiner<C> : AbstractOperation<C, Generation, Generation>, IJoiner
        where C : AbstractRegionJoinerConfig
    {
        protected List<Generation> components = new();

        private readonly int _inputCount = 1;
        public int InputCount { get { return _inputCount; } }
        public bool IsReady => components.Count >= InputCount;

        protected AbstractJoiner(C config, int inputCount) : base(config) 
        {
            _inputCount = inputCount;
        }

        protected override bool RunCondition(Generation input)
        {
            components.Add(input);
            return IsReady;
        }

        protected override Generation Enact(Generation input) {
            Generation main = Join(components);
            JoinGenerationOperationMiliseconds(main, components);
            return main;
        }

        protected abstract Generation Join(List<Generation> inputs);

        protected void JoinGenerationOperationMiliseconds(Generation main, List<Generation> toMergeIn)
        {
            foreach (Generation generation in toMergeIn)
            {
                Dictionary<int, long> operationMillis = generation.GetOperationMillisDictionary();
                foreach (KeyValuePair<int, long> pair in operationMillis)
                {
                    if (generation.GetOperationTime(pair.Key) == -1)
                        generation.AddOperationTime(pair.Key, pair.Value);
                }
            }
        }
    }
}
