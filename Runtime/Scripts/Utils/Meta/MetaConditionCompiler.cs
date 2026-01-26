using Dalichrome.RandomGenerator.Core;
using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
                    if (string.IsNullOrEmpty(cond.key))
                        continue;

                    // Empty string = "presence only" ¨ store null
                    if (string.IsNullOrWhiteSpace(cond.intCondition))
                    {
                        compiled.AddIntCondition(cond.key, null);
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
    }
}
