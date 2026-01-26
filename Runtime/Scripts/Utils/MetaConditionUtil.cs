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

        public bool DoInitialization { get; set; }

        public MetaConditionUtil(IMetaConditionConfig config) : base((AbstractConfig)config)
        {
            this.config = config;
            DoInitialization = true;
        }
        public void Initialize()
        {
            //CreateDistanceMap();
        }
    }
}