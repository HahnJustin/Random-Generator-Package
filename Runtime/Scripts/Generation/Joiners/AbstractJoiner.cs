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
        private int invalidInputs = 0;
        public int InputCount { get { return _inputCount; } }
        public bool IsReady => components.Count + invalidInputs >= InputCount;

        protected AbstractJoiner(C config, int inputCount) : base(config) 
        {
            _inputCount = inputCount;
        }

        protected override bool RunCondition(Generation input)
        {
            if (!input.Valid)
            {
                invalidInputs++;
                input.ParallelDispose();
            }
            else components.Add(input);
            return IsReady;
        }

        protected override Generation Enact(Generation input) {
            // If all generations were invalid
            if (components.Count <= 0) return input;

            // Else join the generations
            Generation main = Join(components);
            JoinGenerationOperationMiliseconds(main, components);
            components.Remove(main);

            foreach (var component in components)
            {
                component.ParallelDispose();
            }

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
