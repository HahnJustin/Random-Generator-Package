#if ODIN_INSPECTOR && UNITY_EDITOR
using Dalichrome.RandomGenerator.Configs;
using Dalichrome.RandomGenerator.Core;
using Dalichrome.RandomGenerator.UserData;
using Sirenix.OdinInspector.Editor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

[DrawerPriority(0, 0, 0)]
public sealed class SerialPairListDrawer : OdinValueDrawer<List<SerialPair<int, int>>>
{
    private readonly Dictionary<string, ReorderableList> _cache = new();

    public override bool CanDrawTypeFilter(Type t)
        => t == typeof(List<SerialPair<int, int>>);

    protected override void DrawPropertyLayout(GUIContent label)
    {
        // Only act when the LIST itself is tagged with [TilePairDisplay]
        var attr = Property.GetAttribute<TilePairDisplayAttribute>();
        if (attr == null)
        {
            CallNextDrawer(label);
            return;
        }

        var list = ValueEntry.SmartValue ?? new List<SerialPair<int, int>>();
        ValueEntry.SmartValue = list;

        // Per-property-path list instance so foldout/selection persists
        string pathKey = Property.Path;
        if (!_cache.TryGetValue(pathKey, out var rl))
        {
            rl = new ReorderableList(list, typeof(SerialPair<int, int>), true, true, true, true);

            rl.drawHeaderCallback = rect => EditorGUI.LabelField(rect, label);
            rl.elementHeight = EditorGUIUtility.singleLineHeight + 4;

            rl.drawElementCallback = (rect, index, active, focused) =>
            {
                if ((uint)index >= (uint)list.Count) return;

                rect.y += 2;
                const int colW = 160;
                const int gap = 6;

                var pair = list[index];

                // Optional: filter by a layer id if your list/property carries one
                int? layerIdFilter = TryGetLayerFilter(Property);

                // Build the items once per repaint; the cache underneath is sticky
                var entry = GenericIdDropdownCache.GetOrBuild(typeof(TileObject), "TileObjects");
                var items = entry.Items;

                if (layerIdFilter.HasValue)
                {
                    items = items.Where(it =>
                    {
                        // CategoryOf[id] holds the "layer" value if present on the asset
                        if (entry.CategoryOf.TryGetValue(it.id, out var cat) && cat is int l) return l == layerIdFilter.Value;
                        return false;
                    }).ToList();
                }

                if (items.Count == 0)
                {
                    // Fallback to int fields if nothing to show
                    pair.Key = EditorGUI.IntField(new Rect(rect.x, rect.y, colW, EditorGUIUtility.singleLineHeight), pair.Key);
                    pair.Value = EditorGUI.IntField(new Rect(rect.x + colW + gap, rect.y, colW, EditorGUIUtility.singleLineHeight), pair.Value);
                    list[index] = pair;
                    return;
                }

                var names = items.Select(i => i.label).ToArray();
                int keyIdx = Math.Max(0, items.FindIndex(i => i.id == pair.Key));
                int valIdx = Math.Max(0, items.FindIndex(i => i.id == pair.Value));

                var keyRect = new Rect(rect.x, rect.y, colW, EditorGUIUtility.singleLineHeight);
                var valRect = new Rect(keyRect.xMax + gap, rect.y, colW, EditorGUIUtility.singleLineHeight);

                // Respect TilePairDisplayAttribute toggles (show dropdown or raw int)
                if (attr.ShowKeyDropdown) keyIdx = EditorGUI.Popup(keyRect, keyIdx, names);
                else pair.Key = EditorGUI.IntField(keyRect, pair.Key);

                if (attr.ShowValueDropdown) valIdx = EditorGUI.Popup(valRect, valIdx, names);
                else pair.Value = EditorGUI.IntField(valRect, pair.Value);

                if (attr.ShowKeyDropdown) pair.Key = items[keyIdx].id;
                if (attr.ShowValueDropdown) pair.Value = items[valIdx].id;

                list[index] = pair; // write-back (struct)
            };

            rl.onAddCallback = r => list.Add(new SerialPair<int, int>(0, 0));
            _cache[pathKey] = rl;
        }

        rl.DoLayoutList();
    }

    /// Attempts to read a layer filter from attributes on the list or its member.
    /// If you still have a 'LimitTileLayerAttribute' that holds an int or enum,
    /// map it to an int here. Return null when no filter applies.
    private static int? TryGetLayerFilter(InspectorProperty property)
    {
        // Example: look for a custom attribute that exposes an int LayerId
        var limitAttr = property.GetAttribute<LimitTileLayerAttribute>();
        if (limitAttr != null) return (int)limitAttr.layer;

        // If your older attribute exposes an enum, translate it here to an int id
        var legacy = property.GetAttribute<LimitTileLayerAttribute>();
        if (legacy != null)
        {
            // TODO: map your legacy enum to an int layer id (via TileLayerInfo or a table)
            // return TileLayerInfo.ResolveLayerId(legacy.layer);
            return null;
        }

        // Or read via reflection on the backing member if you prefer:
        var mi = property.Info.GetMemberInfo();
        var viaMember = mi?.GetCustomAttribute<LimitTileLayerAttribute>(true);
        if (viaMember != null) return (int)viaMember.layer;

        return null;
    }
}
#endif
