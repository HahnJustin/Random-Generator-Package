// UUIDRegistry.cs
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Dalichrome.RandomGenerator.UserData;
using Dalichrome.RandomGenerator.Configs;

public static class UUIDRegistry<T> where T : UUIDScriptableObject
{
    private static readonly object _lock = new();

    // If true, EnsureBuilt will rebuild when the dict exists but is empty.
    // Leave false if "no assets present" is a valid steady state.
    private static bool _rebuildIfEmpty = true;

    private static Dictionary<string, T> _byId;   // non-null => considered "built"
    private static string[] _folders;

    static UUIDRegistry()
    {
        // Get folders from attribute, else default to TypeName
        var attr = (UUIDFolderAttribute)Attribute.GetCustomAttribute(typeof(T), typeof(UUIDFolderAttribute));
        _folders = (attr?.Folders?.Length ?? 0) > 0 ? attr.Folders : new[] { typeof(T).Name };
    }

#if UNITY_EDITOR
    // Clear statics on script reloads / domain reload disabled scenarios
    [UnityEditor.InitializeOnLoadMethod]
    private static void EditorReset()
    {
        ClearCache();
    }
#endif

    // Clear statics when player starts
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RuntimeReset()
    {
        ClearCache();
    }

    /// <summary>Optionally override search folders at runtime (e.g., tests/addressables shim).</summary>
    public static void ConfigureFolders(params string[] resourcesFolders)
    {
        lock (_lock)
        {
            _folders = (resourcesFolders != null && resourcesFolders.Length > 0) ? resourcesFolders : _folders;
            _byId = null; // force rebuild
        }
    }

    public static void ClearCache()
    {
        lock (_lock) _byId = null;
    }

    /// <summary>
    /// Ensure the registry is populated. Thread-safe.
    /// Non-null _byId now guarantees "scanned" because we assign at the end.
    /// </summary>
    public static void EnsureBuilt()
    {
        // Fast path: already built (and, optionally, non-empty)
        var local = _byId;
        if (local != null && (!ShouldRebuildIfEmpty(local))) return;

        lock (_lock)
        {
            if (_byId != null && (!ShouldRebuildIfEmpty(_byId))) return;

            var dict = new Dictionary<string, T>(256);
            var folders = _folders ?? Array.Empty<string>();

            foreach (var folder in folders)
            {
                var all = Resources.LoadAll<T>(folder);
                foreach (var t in all)
                {
                    if (t == null || string.IsNullOrEmpty(t.Uuid)) continue;
                    dict[t.Uuid] = t;
                }
            }

            // Assign at the end so non-null means "built"
            _byId = dict;
        }
    }

    private static bool ShouldRebuildIfEmpty(Dictionary<string, T> dict)
        => _rebuildIfEmpty && dict.Count == 0;

    public static T GetByUuid(string uuid)
    {
        EnsureBuilt();
        var d = _byId; // local snapshot
        if (d == null || string.IsNullOrEmpty(uuid)) return null;
        return d.TryGetValue(uuid, out var t) ? t : null;
    }

    public static IReadOnlyList<(string name, string uuid)> ListNameUuidPairs()
    {
        EnsureBuilt();
        var d = _byId ?? new Dictionary<string, T>(0);
        // Sorted by display name for nice menus
        return d.Values
            .Select(a => (a.DisplayName, a.Uuid))
            .OrderBy(p => p.DisplayName, StringComparer.Ordinal)
            .Select(p => (p.DisplayName, p.Uuid))
            .ToList();
    }
}
