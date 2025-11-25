using System;
using System.Collections.Generic;
using UnityEngine;
using Dalichrome.RandomGenerator.UserData;

/// <summary>
/// General-purpose UUID registry for ScriptableObjects that derive from UUIDScriptableObject.
/// Safe for static use. No InitializeOnLoad attributes on generic types.
/// </summary>
public static class UUIDRegistry<T> where T : UUIDScriptableObject
{
    private static readonly object _lock = new();

    /// <summary>
    /// When true, a registry that built successfully but found no assets
    /// will rebuild next time it's accessed. Turn off if "empty is valid".
    /// </summary>
    private static readonly bool _rebuildIfEmpty = true;

    /// <summary>
    /// Null = not built yet. Non-null = built at least once.
    /// </summary>
    private static Dictionary<string, T> _byId;

    /// <summary>
    /// Folders under Resources/ where these assets are stored.
    /// Determined by UUIDFolderAttribute on T, else defaults to typeof(T).Name.
    /// </summary>
    private static string[] _folders;

    // -------- Static Constructor (safe, not invoked by Unity reflection) --------

    static UUIDRegistry()
    {
        // Look for [UUIDFolder(...)] on T for folder overrides
        var attr = (UUIDFolderAttribute)Attribute.GetCustomAttribute(
            typeof(T),
            typeof(UUIDFolderAttribute)
        );

        _folders = (attr?.Folders?.Length ?? 0) > 0
            ? attr.Folders
            : new[] { typeof(T).Name };
    }

    // -------- Optional Reset Hooks --------
    // These DO NOT use InitializeOnLoadMethod (invalid for generic types)
    // Instead, they rely on UNITY domain reload (normal workflow)
    // And a runtime reset for player.

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RuntimeReset()
    {
        ClearCache();
    }

    /// <summary>
    /// Editor-only reset for domain reload disabled (explicit call).
    /// Call from a non-generic bootstrap class if desired.
    /// </summary>
    internal static void EditorResetSafe()
    {
        ClearCache();
    }

    // -------- Core API --------

    /// <summary>
    /// Clears the internal cache so it will rebuild next access.
    /// </summary>
    public static void ClearCache()
    {
        lock (_lock)
            _byId = null;
    }

    /// <summary>
    /// Builds the registry once, or rebuilds if configured to rebuild on empty.
    /// </summary>
    public static void EnsureBuilt()
    {
        var built = _byId;
        if (built != null && (!_rebuildIfEmpty || built.Count > 0))
            return; // Already built and good enough.

        lock (_lock)
        {
            if (_byId != null && (!_rebuildIfEmpty || _byId.Count > 0))
                return; // Already built inside lock too.

            var dict = new Dictionary<string, T>(256);
            var folders = _folders ?? Array.Empty<string>();

            foreach (var folder in folders)
            {
                var assets = Resources.LoadAll<T>(folder);
                foreach (var asset in assets)
                {
                    if (asset == null || string.IsNullOrEmpty(asset.Uuid))
                        continue;

                    dict[asset.Uuid] = asset;
                }
            }

            _byId = dict; // Non-null means "built"
        }
    }

    /// <summary>
    /// Returns the ScriptableObject for the given UUID, or null if missing.
    /// </summary>
    public static T GetByUuid(string uuid)
    {
        if (string.IsNullOrEmpty(uuid)) return null;

        EnsureBuilt();

        var local = _byId;
        if (local == null) return null;

        return local.TryGetValue(uuid, out var result) ? result : null;
    }

    /// <summary>
    /// Returns pairs (displayName, uuid) sorted alphabetically.
    /// Useful for menus or pickers.
    /// </summary>
    public static IReadOnlyList<(string name, string uuid)> ListNameUuidPairs()
    {
        EnsureBuilt();

        var local = _byId ?? new Dictionary<string, T>(0);
        var list = new List<(string, string)>(local.Count);

        foreach (var val in local.Values)
        {
            list.Add((val.DisplayName, val.Uuid));
        }

        list.Sort((a, b) => string.Compare(a.Item1, b.Item1, StringComparison.Ordinal));
        return list;
    }
}
