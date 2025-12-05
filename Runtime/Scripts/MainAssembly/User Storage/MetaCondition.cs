using Dalichrome.RandomGenerator.Core;
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

        // „Ÿ„Ÿ„Ÿ„Ÿ„Ÿ compiled state (cached, not serialized) „Ÿ„Ÿ„Ÿ„Ÿ„Ÿ

        [NonSerialized] private bool _compiled;
        [NonSerialized] private KeyConditionCompiler.Node _keyExprRoot;
        [NonSerialized] private Dictionary<string, IntConditionCompiler.Node> _intAstByKey;

        /// <summary>Called by registry or at first Matches() to compile all expressions.</summary>
        public void Precompile()
        {
            EnsureCompiled();
        }

        private void EnsureCompiled()
        {
            if (_compiled)
                return;

            // Build per-key int ASTs
            _intAstByKey = new Dictionary<string, IntConditionCompiler.Node>(
                keyIntConditions != null ? keyIntConditions.Count : 0,
                StringComparer.Ordinal);

            if (keyIntConditions != null)
            {
                for (int i = 0; i < keyIntConditions.Count; i++)
                {
                    var cond = keyIntConditions[i];
                    if (string.IsNullOrEmpty(cond.key))
                        continue;

                    // Empty string = "presence only" ¨ store null
                    if (string.IsNullOrWhiteSpace(cond.intCondition))
                    {
                        _intAstByKey[cond.key] = null;
                    }
                    else
                    {
                        _intAstByKey[cond.key] = IntConditionCompiler.Compile(cond.intCondition);
                    }
                }
            }

            // Compile the key expression (may be null = implicit AND mode)
            _keyExprRoot = string.IsNullOrWhiteSpace(keyCondition)
                ? null
                : KeyConditionCompiler.Compile(keyCondition);

            _compiled = true;
        }

#if UNITY_EDITOR && ODIN_INSPECTOR
        [OnValueChanged(nameof(OnEditorChanged))]
        private void OnEditorChanged()
        {
            _compiled = false;  // any inspector edit invalidates the cache
        }
#endif

        public bool Matches(List<MetaPair> metaPairs)
        {
            EnsureCompiled();

            // local helper for value lookup (no allocations)
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
            if (_keyExprRoot == null)
            {
                if (_intAstByKey == null || _intAstByKey.Count == 0)
                    return true; // no conditions at all

                foreach (var kvp in _intAstByKey)
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

                if (_intAstByKey == null ||
                    !_intAstByKey.TryGetValue(keyName, out var node) ||
                    node == null)
                    return true; // presence-only

                return node.Evaluate(value);
            }

            return _keyExprRoot.Evaluate(KeySatisfied);
        }
    }
}