#if ODIN_INSPECTOR
using System;
using System.Collections.Generic;
using System.Reflection;
using Dalichrome.RandomGenerator.Configs;
using Dalichrome.RandomGenerator.Core;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

/*──────────────────────────────────────────────────────────────────────────────╮
│  SerialPairListDrawer                                                       │
│  • Activates only when the LIST itself has [TilePairDisplay].               │
│  • Works even if the list is null or empty.                                 │
│  • Fully IMGUI - no GUILayout in the row callback, so controls render in    │
│    place, not below the list.                                               │
╰──────────────────────────────────────────────────────────────────────────────*/
[DrawerPriority(0, 0, 0)]
public sealed class SerialPairListDrawer
         : OdinValueDrawer<List<SerialPair<int, int>>>
{
    // One ReorderableList per-property path so foldout state is preserved.
    private readonly Dictionary<string, ReorderableList> _cache = new();

    public override bool CanDrawTypeFilter(Type t)
        => t == typeof(List<SerialPair<int, int>>);

    protected override void DrawPropertyLayout(GUIContent label)
    {
        // Activate only if the LIST itself is tagged.
        var attr = Property.GetAttribute<TilePairDisplayAttribute>();
        if (attr == null)
        {
            CallNextDrawer(label);   // let Odin handle un-tagged lists
            return;
        }

        // Ensure list exists (null when first added to a ScriptableObject)
        var list = ValueEntry.SmartValue ?? new List<SerialPair<int, int>>();
        ValueEntry.SmartValue = list;

        // Key for cache
        string pathKey = Property.Path;

        // Create the ReorderableList once per property
        if (!_cache.TryGetValue(pathKey, out var rl))
        {
            rl = new ReorderableList(list, typeof(SerialPair<int, int>),
                                      true, true, true, true); // drag, hdr, add, remove

            // Header
            rl.drawHeaderCallback = rect =>
                EditorGUI.LabelField(rect, label);

            // Row height
            rl.elementHeight = EditorGUIUtility.singleLineHeight + 4;

            // Element drawer
            rl.drawElementCallback = (rect, index, active, focused) =>
            {
                if (index < 0 || index >= list.Count) return;

                rect.y += 2;                                     // baseline align
                const int colW = 140;
                const int gap = 6;

                var pair = list[index];

                // ── KEY column ────────────────────────────────────────────
                var keyRect = new Rect(rect.x, rect.y, colW,
                                       EditorGUIUtility.singleLineHeight);

                var limitAttr = Property.GetAttribute<LimitTileLayerAttribute>();
                if (limitAttr == null)
                {
                    var mi = Property.Info.GetMemberInfo();
                    limitAttr = mi.GetCustomAttribute<LimitTileLayerAttribute>(true);
                }
                LayerType? layerFilter = limitAttr?.layer;

                if (attr.ShowKeyDropdown)
                    pair.Key = TileDropdownOdinUtility.DrawSelector(keyRect, pair.Key,
                                                 limit: layerFilter);
                else
                    pair.Key = EditorGUI.IntField(keyRect, pair.Key);

                // ── VALUE column ──────────────────────────────────────────
                var valRect = new Rect(keyRect.xMax + gap, rect.y,
                                       colW, EditorGUIUtility.singleLineHeight);

                if (attr.ShowValueDropdown)
                    pair.Value = TileDropdownOdinUtility.DrawSelector(valRect, pair.Value,
                             limit: layerFilter);
                else
                    pair.Value = EditorGUI.IntField(valRect, pair.Value);

                list[index] = pair;      // write-back (struct!)
            };

            // Add-item handler (ensures default struct added, not null)
            rl.onAddCallback = r =>
                list.Add(new SerialPair<int, int>(0, 0));

            _cache[pathKey] = rl;
        }

        // Draw the list
        rl.DoLayoutList();
    }
}
#endif