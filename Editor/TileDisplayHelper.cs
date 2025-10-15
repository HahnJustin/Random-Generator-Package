// Editor/TileDisplayHelper.cs
#if UNITY_EDITOR
using Dalichrome.RandomGenerator.Configs;
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

    public static void Invalidate()
    {
        _names = null;
        _ids = null;
        _nextRefresh = 0;
    }

    public static void EnsureCache()
    {
        if (_names != null && EditorApplication.timeSinceStartup < _nextRefresh)
            return;

        var list = new List<(string, int)>();

        // 1) TileObject assets from any Resources folder (Assets/ or Packages/)
        foreach (var obj in Resources.LoadAll<TileObject>(""))
        {
            var path = AssetDatabase.GetAssetPath(obj);
            string prefix = "assets";

            if (!string.IsNullOrEmpty(path))
            {
                if (path.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
                {
                    prefix = "assets";
                }
                else if (path.StartsWith("Packages/", StringComparison.OrdinalIgnoreCase))
                {
                    // Extract "Packages/<pkg-name>/..."
                    // e.g. "Packages/com.yourco.pkg/Resources/TileObjects/..."
                    int start = "Packages/".Length;
                    int slash = path.IndexOf('/', start);
                    prefix = slash > start ? path.Substring(start, slash - start) : "Packages";
                }
            }

            list.Add(($"{prefix}/{obj.name}", obj.id));
        }

        list.Sort((a, b) => string.Compare(a.Item1, b.Item1, StringComparison.OrdinalIgnoreCase));

        _names = list.ConvertAll(t => t.Item1).ToArray();
        _ids = list.ConvertAll(t => t.Item2).ToArray();

        _nextRefresh = EditorApplication.timeSinceStartup + 3.0; // refresh window
    }

    public static void DrawDropdown(SerializedProperty prop, GUIContent label)
    {
        EnsureCache();

        int idx = Array.IndexOf(_ids, prop.intValue);
        if (idx < 0) idx = 0;

        int newIdx = EditorGUILayout.IntPopup(idx, _names, _ids);
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
