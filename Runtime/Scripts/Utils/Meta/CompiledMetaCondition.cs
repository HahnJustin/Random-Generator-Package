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
        private Dictionary<string, FloatConditionNode> keyToFloatNode;
        private Dictionary<int, FloatConditionNode> keyIndexToFloatNode;

        public CompiledMetaCondition(MetaCondition condition)
        {
            _keyCondition = condition.keyCondition;
            keyToFloatNode = new();
            keyIndexToFloatNode = new();
        }

        internal void SetKeyCondition(KeyConditionNode node)
        {
            keyNode = node;
        }

        internal void AddFloatCondition(string key, FloatConditionNode node)
        {
            keyToFloatNode.Add(key, node);
        }

        internal void AddFloatCondition(int keyIndex, FloatConditionNode node)
        {
            keyIndexToFloatNode.Add(keyIndex, node);
        }

        public bool Matches(List<MetaPair> metaPairs)
        {
            // local helper for value lookup
            bool TryGet(string key, out float value)
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
                if (keyToFloatNode == null || keyToFloatNode.Count == 0)
                    return true; // no conditions at all

                foreach (var kvp in keyToFloatNode)
                {
                    string keyName = kvp.Key;
                    var node = kvp.Value; // may be null ¨ presence-only

                    if (!TryGet(keyName, out float value))
                        return false;

                    if (!node.Evaluate(value))
                        return false;
                }

                return true;
            }

            // 2) keyCondition present ¨ use AST, per-key int ASTs
            bool KeySatisfied(string keyName)
            {
                if (!TryGet(keyName, out float value))
                    return false;

                if (keyToFloatNode == null ||
                    !keyToFloatNode.TryGetValue(keyName, out var node) ||
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
                if (keyIndexToFloatNode.Count == 0 && keyToFloatNode.Count == 0)
                    return true;

                foreach (var kvp in keyIndexToFloatNode)
                {
                    int keyIndex = kvp.Key;
                    var node = kvp.Value; // may be null ¨ presence-only

                    float value = g.GetData(pos, keyIndex);

                    if (node != null && !node.Evaluate(value)) return false;
                }

                foreach (var kvp in keyToFloatNode)
                {
                    string key = kvp.Key;
                    var node = kvp.Value; // may be null ¨ presence-only

                    float value = g.GetData(pos, key);

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
                    float value = g.GetData(pos, keyIndex);

                    // Prefer index-bound node if present
                    if (keyIndexToFloatNode != null &&
                        keyIndexToFloatNode.TryGetValue(keyIndex, out var idxNode) &&
                        idxNode != null)
                        return idxNode.Evaluate(value);

                    // Optional fallback to string-bound node if present
                    if (keyToFloatNode != null &&
                        keyToFloatNode.TryGetValue(keyName, out var strNode) &&
                        strNode != null)
                        return strNode.Evaluate(value);

                    return true; // presence-only
                }

                // Fallback to string lookup
                float v2 = g.GetData(pos, keyName);
                if (v2 == 0) return false; // absent

                if (keyToFloatNode != null &&
                    keyToFloatNode.TryGetValue(keyName, out var node) &&
                    node != null)
                    return node.Evaluate(v2);

                return true; // presence-only
            }

            return keyNode.Evaluate(KeySatisfied);
        }
    }
}
