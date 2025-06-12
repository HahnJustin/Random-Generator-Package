// Editor/TileDisplayHelper.cs
#if UNITY_EDITOR
using Dalichrome.RandomGenerator.Configs;
using Dalichrome.RandomGenerator.Core;
using Dalichrome.RandomGenerator.UserData;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

internal static class TileDisplayHelper
{
    private static string[] _names;
    private static int[] _ids;
    private static double _nextRefresh;

    public static void Invalidate()   // callable from the watcher
    {
        _names = null;                 // next EnsureCache() rebuilds everything
        _ids   = null;
        _nextRefresh = 0;
    }

    public static void EnsureCache()
    {
        if (_names != null && EditorApplication.timeSinceStartup < _nextRefresh)
            return;                         // still fresh

        var list = new List<(string, int)>();

        // 1) enum entries
        foreach (TileType e in Enum.GetValues(typeof(TileType)))
            list.Add(($"enum/{e}", (int)e));

        // 2) TileObject assets (Resources/)
        foreach (var obj in Resources.LoadAll<TileObject>(""))
            list.Add(($"asset/{obj.name}", obj.tileId));

        list.Sort((a, b) => string.Compare(a.Item1, b.Item1, StringComparison.OrdinalIgnoreCase));

        _names = list.ConvertAll(t => t.Item1).ToArray();
        _ids = list.ConvertAll(t => t.Item2).ToArray();

        _nextRefresh = EditorApplication.timeSinceStartup + 3.0;   // poll every 3 s
    }

    public static void DrawDropdown(SerializedProperty prop, GUIContent label)
    {
        EnsureCache();

        int idx = Array.IndexOf(_ids, prop.intValue);
        if (idx < 0) idx = 0;

        int newIdx = EditorGUILayout.IntPopup( idx, _names, _ids);
        prop.intValue = _ids[newIdx];
    }

    public static bool HasTileDisplay(UnityEngine.Object target, string fieldName)
    {
        var t = target.GetType();
        var f = t.GetField(fieldName,
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.NonPublic);
        return f != null && Attribute.IsDefined(f, typeof(TileDisplayAttribute));
    }
}
#endif
