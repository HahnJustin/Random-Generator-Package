// Assets/Editor/IntTileListDrawer.cs
#if ODIN_INSPECTOR && UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using Dalichrome.RandomGenerator.Configs;
using Dalichrome.RandomGenerator.UserData;

[DrawerPriority(0, 0, 0)]
public sealed class TileIntListDrawer : OdinValueDrawer<List<int>>
{
    private readonly Dictionary<string, ReorderableList> _cache = new();

    public override bool CanDrawTypeFilter(Type t) => t == typeof(List<int>);

    protected override void DrawPropertyLayout(GUIContent label)
    {
        // Only when the LIST itself has [TileDisplay]
        if (Property.GetAttribute<TileDisplayAttribute>() == null || !ConditionEval.IsVisible(Property))
        {
            CallNextDrawer(label);
            return;
        }

        var list = ValueEntry.SmartValue ??= new List<int>();
        string key = Property.Path;

        if (!_cache.TryGetValue(key, out var rl))
        {
            rl = new ReorderableList(list, typeof(int), true, true, true, true);
            rl.drawHeaderCallback = r => EditorGUI.LabelField(r, label);
            rl.elementHeight = EditorGUIUtility.singleLineHeight + 2f;
            rl.onAddCallback = _ => list.Add(0);

            rl.drawElementCallback = (rect, idx, act, foc) =>
            {
                rect.y += 1f;
                list[idx] = DrawTilePopup(rect, list[idx]);
            };

            _cache[key] = rl;
        }

        rl.DoLayoutList();
    }

    private static int DrawTilePopup(Rect rect, int currentId)
    {
        // Use assets-only cache (Resources/TileObjects), grouped by Assets/Package
        var entry = GenericIdDropdownCache.GetOrBuild(typeof(TileObject), "TileObjects");
        var items = entry.Items;

        if (items.Count == 0)
        {
            // Nothing to show — fallback to raw int
            return EditorGUI.IntField(rect, currentId);
        }

        int idx = Math.Max(0, items.FindIndex(i => i.id == currentId));
        var names = items.Select(i => i.label).ToArray();

        int newIdx = EditorGUI.Popup(rect, idx, names);
        return items[newIdx].id;
    }
}
#endif
