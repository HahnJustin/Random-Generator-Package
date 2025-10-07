#if ODIN_INSPECTOR
using System;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;
using Dalichrome.RandomGenerator.Core;
using Dalichrome.RandomGenerator.UserData;

public static class TileDropdownOdinUtility
{
    private static List<ValueDropdownItem<int>> _cache;
    private static Dictionary<int, LayerType> _layerOf;

    public static IEnumerable<ValueDropdownItem<int>> AllItems
    {
        get
        {
            if (_cache == null) BuildCache();
            return _cache;
        }
    }
    
    public static void Invalidate()        // ← call this when assets change
    {
        _cache   = null;
        _layerOf = null;
    }

    private static IEnumerable<ValueDropdownItem<int>> FilterBy(LayerType? limit)
        => limit == null ? _cache : _cache.Where(i => _layerOf[i.Value] == limit);

    public static int DrawSelector(Rect rect, int current, LayerType? limit = null)
    {
        if (_cache == null) BuildCache();
        var items = FilterBy(limit).ToList();
        var names = items.Select(i => i.Text).ToArray();

        int idx = items.FindIndex(i => i.Value == current);
        if (idx < 0) idx = 0;

        if (Event.current.type == EventType.Repaint && limit != null)
            Debug.Log($"Filter {limit}: {items.Count} items");

        int newIdx = EditorGUI.Popup(rect, idx, names);
        return items[newIdx].Value;
    }

    public static int DrawSelector(GUIContent label, int current, int width = 140,
                                   LayerType? limit = null)
    {
        if (_cache == null) BuildCache();
        var items = FilterBy(limit).ToList();
        var names = items.Select(i => i.Text).ToArray();

        int idx = items.FindIndex(i => i.Value == current);
        if (idx < 0) idx = 0;

        if (Event.current.type == EventType.Repaint && limit != null)
            Debug.Log($"Filter {limit}: {items.Count} items");

        int newIdx = EditorGUILayout.Popup(label, idx, names, GUILayout.Width(width));
        return items[newIdx].Value;
    }

    /*──────────────────────── INTERNALS ────────────────────────────────*/
    private static void BuildCache()
    {
        _cache = new();
        _layerOf = new();

        // enum items -----------------------------------------------------------
        foreach (TileType t in Enum.GetValues(typeof(TileType)))
        {
            int id = (int)t;
            _cache.Add(new($"enum/{t}", id));
            _layerOf[id] = GuessLayer(t.ToString());   // helper below
        }

        // TileObjects in Resources/TileObjects ---------------------------------
        foreach (TileObject so in Resources.LoadAll<TileObject>("TileObjects"))
        {
            int id = so.id;
            if (_cache.All(c => c.Value != id))
                _cache.Add(new($"asset/{so.tileName}", id));

            //_layerOf[id] = so.layer;
        }

        _cache = _cache.OrderBy(i => i.Text, StringComparer.OrdinalIgnoreCase).ToList();
    }

    /* small heuristic for enums/assets when no explicit Layer field */
    private static LayerType GuessLayer(string name)
    {
        if (name.Contains("Wall", StringComparison.OrdinalIgnoreCase)) return LayerType.Wall;
        if (name.Contains("Ground", StringComparison.OrdinalIgnoreCase)) return LayerType.Ground;
        if (name.Contains("Object", StringComparison.OrdinalIgnoreCase)) return LayerType.Object;
        if (name.Contains("Debug", StringComparison.OrdinalIgnoreCase)) return LayerType.Debug;
        return LayerType.NA;
    }

    [MenuItem("Tools/Debug Tile Layers")]
    private static void DebugTileLayers()
    {
        if (_cache == null) BuildCache();
        foreach (var item in _cache.Take(20))                       // first 20
            Debug.Log($"{item.Text}   id={item.Value}   layer={_layerOf[item.Value]}");
    }
}
#endif