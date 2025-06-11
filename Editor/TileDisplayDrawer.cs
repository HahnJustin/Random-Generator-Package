// Editor/TileDisplayDrawer.cs
#if UNITY_EDITOR && !ODIN_INSPECTOR
using System;
using System.Collections.Generic;
using Sirenix.OdinInspector.Editor;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;
using Dalichrome.RandomGenerator.Configs;
using Dalichrome.RandomGenerator.Core;
using Dalichrome.RandomGenerator.UserData;

/// Draws any [TileDisplay] int as a dropdown (enum + TileObject assets).
public sealed class TileDisplayDrawer
    : OdinAttributeDrawer<TileDisplayAttribute, int>
{
    private static List<ValueDropdownItem<int>> cache;

    protected override void DrawPropertyLayout(GUIContent label)
    {
        if (cache == null) BuildCache();

        int current = ValueEntry.SmartValue;
        int index = cache.FindIndex(i => i.Value == current);
        if (index < 0) index = 0;

        string[] names = cache.ConvertAll(i => i.Text).ToArray();
        int newIdx = EditorGUILayout.Popup(label, index, names);

        ValueEntry.SmartValue = cache[newIdx].Value;
    }

    private static void BuildCache()
    {
        cache = new List<ValueDropdownItem<int>>();

        // 1) enum entries
        foreach (TileType e in Enum.GetValues(typeof(TileType)))
            cache.Add(new ValueDropdownItem<int>($"enum/{e}", (int)e));

        // 2) TileObject assets in any Resources/ folder
        foreach (var obj in Resources.LoadAll<TileObject>(""))
            cache.Add(new ValueDropdownItem<int>($"asset/{obj.name}", obj.tileId));
    }

    /* Refresh when assets change */
    private class TileAssetWatcher : AssetPostprocessor
    {
        static void OnPostprocessAllAssets(string[] a, string[] b,
                                           string[] c, string[] d) => cache = null;
    }
}
#endif
