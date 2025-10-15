#if ODIN_INSPECTOR && UNITY_EDITOR
using System;
using System.Linq;
using System.Reflection;
using Dalichrome.RandomGenerator.Configs;
using Dalichrome.RandomGenerator.Core;
using Dalichrome.RandomGenerator.UserData;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

[DrawerPriority(0, 0, 0)]
public sealed class SerialPairOdinDrawer : OdinValueDrawer<SerialPair<int, int>>
{
    public override bool CanDrawTypeFilter(Type t) => t == typeof(SerialPair<int, int>);

    protected override void DrawPropertyLayout(GUIContent label)
    {
        var attr = Property.GetAttribute<TilePairDisplayAttribute>();
        if (attr == null)
        {
            CallNextDrawer(label);
            return;
        }

        var pair = ValueEntry.SmartValue;

        EditorGUILayout.BeginHorizontal();

        // Key column
        if (attr.ShowKeyDropdown)
            pair.Key = DrawTilePopup(pair.Key, 120);
        else
            pair.Key = EditorGUILayout.IntField(pair.Key, GUILayout.Width(80));

        // Value column
        if (attr.ShowValueDropdown)
            pair.Value = DrawTilePopup(pair.Value, 120);
        else
            pair.Value = EditorGUILayout.IntField(pair.Value, GUILayout.Width(80));

        EditorGUILayout.EndHorizontal();

        ValueEntry.SmartValue = pair;
    }

    private static int DrawTilePopup(int currentId, float width)
    {
        var entry = GenericIdDropdownCache.GetOrBuild(typeof(TileObject), "TileObjects");
        var items = entry.Items;

        if (items.Count == 0)
            return EditorGUILayout.IntField(currentId, GUILayout.Width(width));

        int idx = Math.Max(0, items.FindIndex(i => i.id == currentId));
        var names = items.Select(i => i.label).ToArray();

        int newIdx = EditorGUILayout.Popup(idx, names, GUILayout.Width(width));
        return items[newIdx].id;
    }
}
#endif
