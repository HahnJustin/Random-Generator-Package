// Assets/Editor/IntTileListDrawer.cs
#if ODIN_INSPECTOR
using System;
using System.Collections.Generic;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using Dalichrome.RandomGenerator.Configs;
using Dalichrome.RandomGenerator.Core;

[DrawerPriority(0, 0, 0)]
public sealed class TileIntListDrawer : OdinValueDrawer<List<int>>
{
    private readonly Dictionary<string, ReorderableList> cache = new();

    public override bool CanDrawTypeFilter(Type t) => t == typeof(List<int>);

    protected override void DrawPropertyLayout(GUIContent label)
    {
        // Only touch lists that have [TileDisplay] on THEMSELVES
        if (Property.GetAttribute<TileDisplayAttribute>() == null ||
            !ConditionEval.IsVisible(Property))          // obey Condition
        {
            CallNextDrawer(label);
            return;
        }

        var list = ValueEntry.SmartValue ??= new List<int>();
        string key = Property.Path;

        if (!cache.TryGetValue(key, out var rl))
        {
            rl = new ReorderableList(list, typeof(int), true, true, true, true);
            rl.drawHeaderCallback = r => EditorGUI.LabelField(r, label);
            rl.elementHeight = EditorGUIUtility.singleLineHeight + 2;
            rl.onAddCallback = _ => list.Add(0);

            var limitAttr = Property.GetAttribute<LimitTileLayerAttribute>();
            LayerType? layerFilter = limitAttr?.layer;

            rl.drawElementCallback = (rect, idx, act, foc) =>
            {
                rect.y += 1;
                list[idx] = TileDropdownOdinUtility.DrawSelector(rect, list[idx], layerFilter);
            };
            cache[key] = rl;
        }

        rl.DoLayoutList();
    }
}
#endif