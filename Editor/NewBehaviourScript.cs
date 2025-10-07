#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Dalichrome.RandomGenerator.Configs;

internal static class GenericIdDropdownCache
{
    // cache key
    private readonly struct Key : IEquatable<Key>
    {
        public readonly Type EnumType;
        public readonly Type ResType;
        public readonly string Path;
        public Key(Type e, Type r, string p) { EnumType = e; ResType = r; Path = p ?? ""; }
        public bool Equals(Key other) =>
            EnumType == other.EnumType && ResType == other.ResType && Path == other.Path;
        public override bool Equals(object o) => o is Key k && Equals(k);
        public override int GetHashCode() => HashCode.Combine(EnumType, ResType, Path);
    }

    internal sealed class Entry
    {
        // Ordered list shown in UI
        public List<(string label, int id)> Items = new();
        // Optional category per id (used for filtering). Object because enum type varies.
        public Dictionary<int, object> CategoryOf = new();
        // Bucket of assets per id (lets us exclude ÅgselfÅh)
        public Dictionary<int, List<ScriptableObject>> AssetsById = new();
    }

    private static readonly Dictionary<Key, Entry> _cache = new();
    private static readonly object _lock = new();

    static GenericIdDropdownCache()
    {
        // Invalidate on common editor events
        Undo.undoRedoPerformed += ClearAll;
        EditorApplication.projectChanged += ClearAll;
        AssemblyReloadEvents.afterAssemblyReload += ClearAll;

        // Invalidate when any AbstractUserData id changes (your earlier signal)
        AbstractUserData.AnyChanged += _ => ClearAll();
    }

    private static void ClearAll()
    {
        lock (_lock) _cache.Clear();
    }

    public static Entry GetOrBuild(Type enumType, Type resType, string path,
                                   string enumPrefix, string assetPrefix)
    {
        var key = new Key(enumType, resType, path);
        lock (_lock)
        {
            if (_cache.TryGetValue(key, out var entry))
                return entry;

            entry = Build(enumType, resType, path, enumPrefix, assetPrefix);
            _cache[key] = entry;
            return entry;
        }
    }

    private static Entry Build(Type enumType, Type resType, string path,
                               string enumPrefix, string assetPrefix)
    {
        var e = new Entry();

        // 1) enum members
        foreach (var v in Enum.GetValues(enumType))
        {
            int id = Convert.ToInt32(v);
            string name = v.ToString();
            e.Items.Add(($"{enumPrefix}{name}", id));
            // Category from name heuristic (optional); leave null by default
        }

        // 2) resource assets
        var assets = Resources.LoadAll(path, resType);
        foreach (var obj in assets)
        {
            var so = (ScriptableObject)obj;

            // we need int id & (optional) category from the asset instance
            // Convention: asset derives from AbstractUserData and implements GetId()
            int id = (so as AbstractUserData)?.GetId() ?? default;

            // Label: use name or a domain-specific field if it exists
            string label = $"{assetPrefix}{so.name}";
            e.Items.Add((label, id));

            // Group assets per id (for excluding ÅgselfÅh later)
            if (!e.AssetsById.TryGetValue(id, out var list)) e.AssetsById[id] = list = new();
            list.Add(so);

            // Optional: infer a category if the asset has a 'layer' field/prop of enum type
            // This is generic: if res has a member named "layer" matching some enum.
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

        // stable ordering
        e.Items.Sort((a, b) => string.Compare(a.label, b.label, StringComparison.OrdinalIgnoreCase));
        return e;
    }

    /// Exclude the currently edited object(s) from the collision set, return hit label if others remain.
    public static bool TryGetOtherAssetHits(Entry entry, int id, UnityEngine.Object[] currentTargets, out string names)
    {
        names = null;
        if (entry.AssetsById.TryGetValue(id, out var bucket) && bucket != null && bucket.Count > 0)
        {
            var self = new HashSet<UnityEngine.Object>(currentTargets);
            var others = bucket.Where(o => !self.Contains(o)).ToList();
            if (others.Count > 0)
            {
                names = string.Join(", ", others.Select(o => o.name));
                return true;
            }
        }
        return false;
    }
}
#endif
