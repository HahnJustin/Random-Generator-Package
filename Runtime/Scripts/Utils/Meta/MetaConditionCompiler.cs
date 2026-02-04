
using Dalichrome.RandomGenerator.Configs;
using Dalichrome.RandomGenerator.Core;
using System.Collections.Generic;
using Unity.Collections;

namespace Dalichrome.RandomGenerator.Utils
{
    public static class MetaConditionCompiler
    {
        public static CompiledMetaCondition Compile(MetaCondition metaCondition)
        {
            CompiledMetaCondition compiled = new (metaCondition);

            if (metaCondition.keyIntConditions != null)
            {
                for (int i = 0; i < metaCondition.keyIntConditions.Count; i++)
                {
                    var cond = metaCondition.keyIntConditions[i];
                    if (string.IsNullOrEmpty(cond.key) || string.IsNullOrWhiteSpace(cond.intCondition))
                        continue;

                    compiled.AddIntCondition(cond.key, IntConditionCompiler.Compile(cond.intCondition));
                }
            }

            // Compile the key expression (may be null = implicit AND mode)
            KeyConditionNode keyNode = string.IsNullOrWhiteSpace(compiled.KeyCondition)
                ? null
                : KeyConditionCompiler.Compile(compiled.KeyCondition);
            compiled.SetKeyCondition(keyNode);

            return compiled;
        }

        public static CompiledMetaCondition Compile(MetaCondition metaCondition, TileGrid grid)
        {
            CompiledMetaCondition compiled = new(metaCondition);

            if (metaCondition.keyIntConditions != null)
            {
                for (int i = 0; i < metaCondition.keyIntConditions.Count; i++)
                {
                    var cond = metaCondition.keyIntConditions[i];
                    if (string.IsNullOrEmpty(cond.key) || string.IsNullOrWhiteSpace(cond.intCondition))
                        continue;

                    // Empty string = "presence only" ¨ store null
                    int index = grid.GetMetaIndex(cond.key);
                    if (index != -1)
                    {
                        compiled.AddIntCondition(index, IntConditionCompiler.Compile(cond.intCondition));
                    }
                    else
                    {
                        compiled.AddIntCondition(cond.key, IntConditionCompiler.Compile(cond.intCondition));
                    }
                }
            }

            // Compile the key expression (may be null = implicit AND mode)
            KeyConditionNode keyNode = string.IsNullOrWhiteSpace(compiled.KeyCondition)
                ? null
                : KeyConditionCompiler.Compile(compiled.KeyCondition);
            compiled.SetKeyCondition(keyNode);

            return compiled;
        }

        public static List<FixedString64Bytes> ExtractKeys(MetaCondition condition)
        {
            if (condition == null) return new List<FixedString64Bytes>(0);

            var set = new HashSet<FixedString64Bytes>();

            if (condition.keyIntConditions != null)
            {
                foreach (var c in condition.keyIntConditions)
                {
                    if (!string.IsNullOrWhiteSpace(c.key))
                        set.Add(new FixedString64Bytes(c.key));
                }
            }

            foreach (var k in KeyConditionCompiler.ExtractKeys(condition.keyCondition))
                set.Add(k);

            return new List<FixedString64Bytes>(set);
        }
    }
}
