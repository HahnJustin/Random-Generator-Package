using Dalichrome.RandomGenerator.Core;
using Dalichrome.RandomGenerator.Configs;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Dalichrome.RandomGenerator.Utils
{
    public class CompiledMetaCondition
    {
        private string _keyCondition;
        public string KeyCondition { get { return _keyCondition; } }

        private KeyConditionNode keyNode;
        private Dictionary<string, IntConditionNode> keyToIntNode;

        public CompiledMetaCondition(MetaCondition condition)
        {
            _keyCondition = condition.keyCondition;
            keyToIntNode = new(
                condition.keyIntConditions != null ? condition.keyIntConditions.Count : 0,
                StringComparer.Ordinal);
        }

        internal void SetKeyCondition(KeyConditionNode node)
        {
            keyNode = node;
        }

        internal void AddIntCondition(string key, IntConditionNode node)
        {
            keyToIntNode.Add(key, node);
        }

        public bool Matches(List<MetaPair> metaPairs)
        {
            // local helper for value lookup
            bool TryGet(string key, out int value)
            {
                if (metaPairs != null)
                {
                    for (int i = 0; i < metaPairs.Count; i++)
                    {
                        if (metaPairs[i].field == key)
                        {
                            value = metaPairs[i].value;
                            return true;
                        }
                    }
                }

                value = default;
                return false;
            }

            // 1) No keyCondition ¨ implicit AND of all keyIntConditions
            if (keyNode == null)
            {
                if (keyToIntNode == null || keyToIntNode.Count == 0)
                    return true; // no conditions at all

                foreach (var kvp in keyToIntNode)
                {
                    string keyName = kvp.Key;
                    var node = kvp.Value; // may be null ¨ presence-only

                    if (!TryGet(keyName, out int value))
                        return false;

                    if (node == null)
                        continue; // presence-only ok

                    if (!node.Evaluate(value))
                        return false;
                }

                return true;
            }

            // 2) keyCondition present ¨ use AST, per-key int ASTs
            bool KeySatisfied(string keyName)
            {
                if (!TryGet(keyName, out int value))
                    return false;

                if (keyToIntNode == null ||
                    !keyToIntNode.TryGetValue(keyName, out var node) ||
                    node == null)
                    return true; // presence-only

                return node.Evaluate(value);
            }

            return keyNode.Evaluate(KeySatisfied);
        }
    }
}
