#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

internal static class GenericIdDropdownCache
{
    private readonly struct Key : IEquatable<Key>
    {
        public readonly Type ResType;
        public readonly string Path;
        public Key(Type r, string p) { ResType = r; Path = p ?? ""; }
        public bool Equals(Key o) => ResType == o.ResType && Path == o.Path;
        public override bool Equals(object o) => o is Key k && Equals(k);
        public override int GetHashCode() => HashCode.Combine(ResType, Path);
    }

    internal sealed class Entry
    {
        public List<(string label, int id)> Items = new();
        public Dictionary<int, object> CategoryOf = new();
        public Dictionary<int, List<ScriptableObject>> AssetsById = new();
    }

    private static readonly Dictionary<Key, Entry> _cache = new();
    private static readonly object _lock = new();

    static GenericIdDropdownCache()
    {
        Undo.undoRedoPerformed += ClearAll;
        EditorApplication.projectChanged += ClearAll;
        AssemblyReloadEvents.afterAssemblyReload += ClearAll;
        AbstractUserData.AnyChanged += _ => ClearAll();
    }
    private static void ClearAll() { lock (_lock) _cache.Clear(); }

    // NEW: assets-only entry point
    public static Entry GetOrBuild(Type resType, string resourcesPath)
    {
        var key = new Key(resType, resourcesPath);
        lock (_lock)
        {
            if (_cache.TryGetValue(key, out var e)) return e;
            var built = BuildAssetsOnly(resType, resourcesPath);
            _cache[key] = built;
            return built;
        }
    }

    private static Entry BuildAssetsOnly(Type resType, string resourcesPath)
    {
        var e = new Entry();

        // Load from ALL Resources (project + every package)
        var assets = Resources.LoadAll(resourcesPath, resType).Cast<ScriptableObject>();

        // If both package & project provide the same ID, prefer project
        var pick = new Dictionary<int, ScriptableObject>();
        foreach (var so in assets)
        {
            int id = (so as AbstractUserData)?.GetId() ?? default;
            string path = AssetDatabase.GetAssetPath(so);
            bool isProject = path.StartsWith("Assets/");
            if (!pick.TryGetValue(id, out var cur))
            {
                pick[id] = so;
            }
            else
            {
                var curPath = AssetDatabase.GetAssetPath(cur);
                bool curIsProject = curPath.StartsWith("Assets/");
                if (isProject && !curIsProject) pick[id] = so;
            }
        }

        foreach (var kv in pick)
        {
            var so = kv.Value;
            int id = kv.Key;

            string tab = GetSourceTabName(so);               // "Assets" or "<Package Display Name>"
            string label = $"{tab}/{so.name}";
            e.Items.Add((label, id));

            if (!e.AssetsById.TryGetValue(id, out var list)) e.AssetsById[id] = list = new();
            list.Add(so);

            // Optional: capture a category (e.g., 'layer') for later filtering
            var layerMember = resType.GetMember("layer").FirstOrDefault();
            if (layerMember != null)
            {
                object cat = null;
                switch (layerMember.MemberType)
                {
                    case System.Reflection.MemberTypes.Field:
                        cat = ((System.Reflection.FieldInfo)layerMember).GetValue(so);
                        break;
                    case System.Reflection.MemberTypes.Property:
                        var pi = (System.Reflection.PropertyInfo)layerMember;
                        if (pi.CanRead) cat = pi.GetValue(so);
                        break;
                }
                if (cat != null) e.CategoryOf[id] = cat;
            }
        }

        e.Items.Sort((a, b) => string.Compare(a.label, b.label, StringComparison.OrdinalIgnoreCase));
        return e;
    }

    private static string GetSourceTabName(ScriptableObject so)
    {
        var path = AssetDatabase.GetAssetPath(so);
        if (string.IsNullOrEmpty(path)) return "Assets";
        if (path.StartsWith("Assets/"))
        {
            // **Option A (simple):** always "Assets"
            return "Assets";

            // **Option B (granular):** nearest 'Resources' parent or top-level folder under Assets
            // return GetAssetsGroupByResourcesRoot(path); 
        }
        if (path.StartsWith("Packages/"))
        {
            var info = UnityEditor.PackageManager.PackageInfo.FindForAssetPath(path);
            if (info != null && !string.IsNullOrWhiteSpace(info.displayName))
                return info.displayName;
            if (info != null) return info.name;

            var m = Regex.Match(path, @"^Packages/([^/]+)/");
            return m.Success ? m.Groups[1].Value : "Package";
        }
        return "Assets";
    }

    // Optional granularity for project items:
    private static string GetAssetsGroupByResourcesRoot(string assetPath)
    {
        // e.g., Assets/MyGame/Art/Resources/TileObjects/Foo.asset -> "MyGame/Art"
        var parts = assetPath.Split('/');
        int idx = Array.IndexOf(parts, "Resources");
        if (idx > 1) return string.Join("/", parts.Skip(1).Take(idx - 1)); // skip "Assets"
        // fallback to first folder under Assets
        return parts.Length > 2 ? parts[1] : "Assets";
    }

    // unchanged: TryGetOtherAssetHits(...)
}
#endif
