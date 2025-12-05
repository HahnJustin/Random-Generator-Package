using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Dalichrome.RandomGenerator.UserData
{
    [Serializable]
    public class MetaCondition
    {
#if ODIN_INSPECTOR
        [LabelText("Key Expr")]
        [PropertyOrder(0)]
#endif
        public string keyCondition;

#if ODIN_INSPECTOR
        [PropertyOrder(1)]
        [TableList(AlwaysExpanded = true,
                   DrawScrollView = false,
                   ShowIndexLabels = false)]
        [LabelText("Per-key Conditions")]
#endif
        public List<MetaKeyIntCondition> keyIntConditions = new();

        public bool Matches(IReadOnlyDictionary<string, int> context)
        {
            // Build lookup of per-key int expressions
            var map = new Dictionary<string, string>(StringComparer.Ordinal);
            for (int i = 0; i < keyIntConditions.Count; i++)
            {
                var cond = keyIntConditions[i];
                if (!string.IsNullOrEmpty(cond.key))
                    map[cond.key] = cond.intCondition;
            }

            // „Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ
            // 1. No keyCondition ¨ implicit AND of all int conditions
            // „Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ
            if (string.IsNullOrWhiteSpace(keyCondition))
            {
                foreach (var kvp in map)
                {
                    string keyName = kvp.Key;
                    string expr = kvp.Value;

                    // Key must exist
                    if (!context.TryGetValue(keyName, out int value))
                        return false;

                    // Empty intCondition = "just needs to exist"
                    if (string.IsNullOrWhiteSpace(expr))
                        continue;

                    if (!IntConditionEvaluator.Evaluate(expr, value))
                        return false;
                }

                // If there are no keyIntConditions at all, this evaluates to true
                return true;
            }

            // „Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ
            // 2. keyCondition present ¨ use key grammar
            // „Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ
            bool KeySatisfied(string keyName)
            {
                // Key must exist
                if (!context.TryGetValue(keyName, out int value))
                    return false;

                // If no numeric condition for this key, presence is enough
                if (!map.TryGetValue(keyName, out var expr) ||
                    string.IsNullOrWhiteSpace(expr))
                    return true;

                return IntConditionEvaluator.Evaluate(expr, value);
            }

            return KeyConditionEvaluator.Evaluate(keyCondition, KeySatisfied);
        }
    }
}