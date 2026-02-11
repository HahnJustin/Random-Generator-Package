using Dalichrome.RandomGenerator.Configs;
using Dalichrome.RandomGenerator.Data;
using Dalichrome.RandomGenerator.Utils;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Mathematics;

namespace Dalichrome.RandomGenerator.Generators
{
    public class MetaInitializer : AbstractInitializer
    {
        protected override bool RunCondition()
        {
            bool containsHotMeta = false;

            for (int i = configs.Count - 1; i >= 0; i--) 
            {
                AbstractConfig config = configs[i];
                if ((config is not IMetaKeyConfig &&
                    config is not IMetaFunctionConfig &&
                    config is not IMetaConditionConfig) ||
                    !config.Enabled)
                    configs.RemoveAt(i);
                else { containsHotMeta = true; }
            }

            return containsHotMeta;
        }

        protected override Generation Do(Generation generation)
        {
            var hot = new HashSet<FixedString64Bytes>();

            foreach (AbstractConfig config in configs)
            {
                if (config is IMetaKeyConfig keyConfig)
                    hot.Add(new FixedString64Bytes(keyConfig.MetaKey));

                if (config is IMetaFunctionConfig funcConfig)
                {
                    foreach (var k in MetaFunctionCompiler.ExtractKeys(funcConfig.MetaFunction))
                        hot.Add(k);
                }

                if (config is IMetaConditionConfig condConfig)
                {
                    foreach (var k in MetaConditionCompiler.ExtractKeys(condConfig.MetaCondition))
                        hot.Add(k);
                }
            }

            tileGrid.InitializeMeta(hot.ToList());

            return generation;
        }
    }
}