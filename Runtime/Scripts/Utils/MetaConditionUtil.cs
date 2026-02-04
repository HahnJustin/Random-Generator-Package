using UnityEngine;
using Dalichrome.RandomGenerator.Configs;
using Dalichrome.RandomGenerator.Core;
using System.Collections.Generic;
using Unity.Mathematics;

namespace Dalichrome.RandomGenerator.Utils
{
    public class MetaConditionUtil : AbstractUtil, IInitializableUtil
    {
        protected new IMetaConditionConfig config;

        private CompiledMetaCondition compiledCondition;

        public bool DoInitialization { get; set; }

        public MetaConditionUtil(IMetaConditionConfig config) : base((AbstractConfig)config)
        {
            this.config = config;
            DoInitialization = true;
        }

        private void Compile()
        {
            compiledCondition = MetaConditionCompiler.Compile(config.MetaCondition);
        }

        public void Initialize()
        {
            Compile();
        }

        public bool MatchMetaCondition(List<MetaPair> metaPairs)
        {
            if (compiledCondition == null) return false;

            return compiledCondition.Matches(metaPairs);
        }

        public bool MatchMetaCondition(int x, int y)
        {
            if (compiledCondition == null) return false;

            return compiledCondition.Matches(ref tileGrid, x, y);
        }
    }
}