using Dalichrome.RandomGenerator.Configs;
using Dalichrome.RandomGenerator.Core;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Mathematics;
using Unity.VisualScripting.YamlDotNet.Core.Tokens;
using UnityEngine;

namespace Dalichrome.RandomGenerator.Utils
{
    public class CompiledMetaCondition
    {
        private string _keyCondition;
        public string KeyCondition { get { return _keyCondition; } }

        private KeyConditionNode keyNode;
        private Dictionary<string, IntConditionNode> keyToIntNode;
        private Dictionary<int, IntConditionNode> keyIndexToIntNode;

        public CompiledMetaCondition(MetaCondition condition)
        {
            _keyCondition = condition.keyCondition;
            keyToIntNode = new();
            keyIndexToIntNode = new();
        }

        internal void SetKeyCondition(KeyConditionNode node)
        {
            keyNode = node;
        }

        internal void AddIntCondition(string key, IntConditionNode node)
        {
            keyToIntNode.Add(key, node);
        }

        internal void AddIntCondition(int keyIndex, IntConditionNode node)
        {
            keyIndexToIntNode.Add(keyIndex, node);
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Matches(ref TileGrid grid, int x, int y)
        {
            int2 pos = new int2(x, y);

            // IMPORTANT: you cannot capture a ref parameter in a lambda/local function.
            // Copy to a non-ref local first.
            TileGrid g = grid;

            if (keyNode == null)
            {
                if (keyIndexToIntNode.Count == 0 && keyToIntNode.Count == 0)
                    return true;

                foreach (var kvp in keyIndexToIntNode)
                {
                    int keyIndex = kvp.Key;
                    var node = kvp.Value; // may be null ¨ presence-only

                    int value = g.GetData(pos, keyIndex);

                    if (value == 0) return false;          // absent (your convention)
                    if (node != null && !node.Evaluate(value)) return false;
                }

                foreach (var kvp in keyToIntNode)
                {
                    string key = kvp.Key;
                    var node = kvp.Value; // may be null ¨ presence-only

                    int value = g.GetData(pos, key);

                    if (value == 0) return false;          // absent
                    if (node != null && !node.Evaluate(value)) return false;
                }

                return true;
            }

            bool KeySatisfied(string keyName)
            {
                // Try hot meta index first
                int keyIndex = g.GetMetaIndex((FixedString64Bytes)keyName);
                if (keyIndex != -1)
                {
                    int value = g.GetData(pos, keyIndex);
                    if (value == 0) return false; // absent

                    // Prefer index-bound node if present
                    if (keyIndexToIntNode != null &&
                        keyIndexToIntNode.TryGetValue(keyIndex, out var idxNode) &&
                        idxNode != null)
                        return idxNode.Evaluate(value);

                    // Optional fallback to string-bound node if present
                    if (keyToIntNode != null &&
                        keyToIntNode.TryGetValue(keyName, out var strNode) &&
                        strNode != null)
                        return strNode.Evaluate(value);

                    return true; // presence-only
                }

                // Fallback to string lookup
                int v2 = g.GetData(pos, keyName);
                if (v2 == 0) return false; // absent

                if (keyToIntNode != null &&
                    keyToIntNode.TryGetValue(keyName, out var node) &&
                    node != null)
                    return node.Evaluate(v2);

                return true; // presence-only
            }

            return keyNode.Evaluate(KeySatisfied);
        }
    }
}
